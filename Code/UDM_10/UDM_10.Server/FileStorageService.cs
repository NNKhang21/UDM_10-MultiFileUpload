using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using UDM_10.Shared.Config;
using UDM_10.Shared.Models;
using UDM_10.Shared.Protocol;
namespace UDM_10.Server;

// NOTE (fix): bản gốc chỉ có skeleton (PrepareUploadAsync/ReceiveFileAsync...)
// nhưng ClientSession.cs (Trần Hữu Nam) lại gọi BeginUploadAsync/WriteChunkAsync/
// FinishUploadAsync -> build lỗi vì thiếu method. Bổ sung tối thiểu 3 method này
// để chạy được luồng UploadStart -> Chunk -> Done. Cũng đã bỏ ServerEvent (type
// không tồn tại trong UDM_10.Shared) và đổi sang Logger.Warn(string) thường.
//
// GIỚI HẠN: _current chỉ lưu MỘT upload tại một thời điểm vì FileStorageService
// dùng chung cho toàn Server (xem Program.cs). Nếu 2 client upload đồng thời,
// state này sẽ đè lên nhau. Đủ để test 1 client, nhưng cần tách state theo
// session/connection trước khi cho nhiều client upload song song thật.
public class FileStorageService
{
    private const int MaxFileNameLength = 255;

    private static readonly string[] _windowsReservedNames =
    {
        "CON","PRN","AUX","NUL",  "COM1",  "COM2","COM3","COM4", "COM5","COM6", "COM7", "COM8",
        "COM9", "LPT1",  "LPT2",  "LPT3", "LPT4",  "LPT5",  "LPT6", "LPT7", "LPT8", "LPT9"
    };
    private static readonly SemaphoreSlim _nameLock = new(1, 1);

    private readonly ServerConfig _config;

    // State của upload đang xử lý (xem GIỚI HẠN ở trên).
    private UploadState? _current;

    public FileStorageService(ServerConfig config)
    {
        _config = config;
        Directory.CreateDirectory(_config.UploadDirectory);
    }

    private sealed class UploadState
    {
        public required string FileName { get; init; }
        public required string TargetPath { get; init; }
        public required string PartPath { get; init; }
        public required FileStream PartFile { get; init; }
        public required long ExpectedSize { get; init; }
        public long BytesWritten { get; set; }
    }

    #region Validation

    public void ValidateFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            Logger.Warn($"[Validation Failed] Empty filename");
            throw new ArgumentException("Empty filename");
        }

        if (fileName.Length > MaxFileNameLength)
        {
            Logger.Warn($"[Validation Failed] Filename too long: {fileName} ({fileName.Length})");
            throw new ArgumentException("Filename too long");
        }

        if (fileName.Contains("..") || Path.IsPathRooted(fileName))
        {
            Logger.Warn($"[Validation Failed] Invalid filename (path traversal): {fileName}");
            throw new ArgumentException("Invalid filename (path traversal)");
        }

        if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            Logger.Warn($"[Validation Failed] Invalid characters in filename: {fileName}");
            throw new ArgumentException("Invalid characters in filename");
        }

        var nameOnly = Path.GetFileNameWithoutExtension(fileName).ToUpperInvariant();
        for (int i = 0; i < _windowsReservedNames.Length; i++)
        {
            if (nameOnly == _windowsReservedNames[i])
            {
                Logger.Warn($"[Validation Failed] Reserved filename: {fileName}");
                throw new ArgumentException("Reserved filename");
            }
        }
    }

    public void ValidateFileSize(long fileSize)
    {
        long maxBytes = (long)_config.MaxFileSizeMb * 1024 * 1024;
        if (fileSize <= 0 || fileSize > maxBytes)
        {
            Logger.Warn($"[Validation Failed] Invalid file size: {fileSize} (max {maxBytes})");
            throw new ArgumentException($"Size {fileSize} exceeds limit {maxBytes} bytes");
        }
    }

    public void ValidateChunkLength(long declaredLength, long remainingBytes)
    {
        if (declaredLength <= 0 || declaredLength > remainingBytes)
        {
            Logger.Warn($"[Validation Failed] Invalid chunk length: {declaredLength} (remaining {remainingBytes})");
            throw new ArgumentException($"Invalid chunk length {declaredLength}, remaining {remainingBytes}");
        }
    }

    #endregion

    #region Upload Preparation

    public string ResolveFinalPath(string fileName)
    {
        var targetPath = Path.Combine(_config.UploadDirectory, fileName);

        if (File.Exists(targetPath))
        {
            Logger.Warn($"[Validation Failed] File already exists: {fileName}");
            throw new IOException($"File already exists: {fileName}");
        }

        return targetPath;
    }

    public async Task<(string TargetPath, FileStream PartFile)> PrepareUploadAsync(string fileName, CancellationToken ct = default)
    {
        await _nameLock.WaitAsync(ct);

        try
        {
            string targetPath = ResolveFinalPath(fileName);
            string partPath = targetPath + ".part";

            FileStream partFile = new FileStream(
                partPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None);

            return (targetPath, partFile);
        }
        finally
        {
            _nameLock.Release();
        }
    }

    private static string GenerateDuplicateName(string targetPath)
    {
        string dir = Path.GetDirectoryName(targetPath) ?? string.Empty;
        string name = Path.GetFileNameWithoutExtension(targetPath);
        string ext = Path.GetExtension(targetPath);

        int i = 1;
        string candidate;

        do
        {
            candidate = Path.Combine(dir, $"{name}({i}){ext}");
            i++;
        }
        while (File.Exists(candidate) || File.Exists(candidate + ".part"));

        return candidate;
    }

    #endregion

    #region Upload Process (API dùng bởi ClientSession.cs)

    // UploadStart -> tạo .part file, lưu state cho lần chunk/done tiếp theo.
    public async Task BeginUploadAsync(UploadStartMessage start)
    {
        ValidateFileName(start.FileName);
        ValidateFileSize(start.FileSize);

        var (targetPath, partFile) = await PrepareUploadAsync(start.FileName);

        _current = new UploadState
        {
            FileName = start.FileName,
            TargetPath = targetPath,
            PartPath = targetPath + ".part",
            PartFile = partFile,
            ExpectedSize = start.FileSize,
            BytesWritten = 0
        };

        Logger.Info($"[Storage] Begin upload '{start.FileName}' ({start.FileSize} bytes)");
    }

    // UploadChunk -> giải mã base64, ghi nối tiếp vào .part file.
    public async Task WriteChunkAsync(UploadChunkMessage chunk)
    {
        if (_current == null)
        {
            throw new InvalidOperationException("Received chunk before UploadStart.");
        }

        byte[] buffer = Convert.FromBase64String(chunk.DataBase64);

        long remaining = _current.ExpectedSize - _current.BytesWritten;

        ValidateChunkLength(buffer.Length, remaining);

        await WriteChunkToPartFile(_current.PartFile, buffer, CancellationToken.None);

        _current.BytesWritten += buffer.Length;

        Logger.Info($"[Storage] Chunk {chunk.ChunkIndex} written ({buffer.Length} bytes, total {_current.BytesWritten}/{_current.ExpectedSize})");
    }

    // UploadDone -> kiểm tra đủ dữ liệu, đóng .part và đổi tên thành file thật.
    public async Task<bool> FinishUploadAsync(UploadDoneMessage done)
    {
        if (_current == null)
        {
            throw new InvalidOperationException("Received UploadDone before UploadStart.");
        }

        var state = _current;

        try
        {
            VerifyUpload(state.BytesWritten, state.ExpectedSize);

            await state.PartFile.FlushAsync();
            state.PartFile.Close();

            CompleteUpload(state.TargetPath);

            Logger.Info($"[Storage] Upload finished: {state.FileName}");

            return true;
        }
        catch (Exception ex)
        {
            Logger.Warn($"[Storage] Upload failed for '{state.FileName}': {ex.Message}");

            try
            {
                state.PartFile.Close();
            }
            catch
            {
                // đã đóng hoặc lỗi khác, bỏ qua để không che lấp lỗi gốc
            }

            RollbackUpload(state.TargetPath);

            return false;
        }
        finally
        {
            _current = null;
        }
    }

    public async Task<string> ReceiveFileAsync(Stream stream, string targetPath, FileStream partFile, long expectedSize, CancellationToken ct, int idleTimeoutMs = 0)
    {
        // TODO: dùng cho hướng đọc raw byte (ReadChunkHeader/ReadChunkData) thay
        // vì base64-trong-JSON như hiện tại. Chưa được ClientSession.cs gọi tới.
        return await Task.FromResult(string.Empty);
    }

    private async Task<UploadChunkHeader> ReadChunkHeader(Stream stream, CancellationToken ct, int idleTimeoutMs)
    {
        // TODO: read JSON header from stream via MessageFramer, deserialize into UploadChunkHeader
        return await Task.FromResult<UploadChunkHeader>(null!);
    }

    private async Task<byte[]> ReadChunkData(Stream stream, int chunkLength, CancellationToken ct, int idleTimeoutMs)
    {
        // TODO: read raw data from stream via MessageFramer according to chunkLength
        return await Task.FromResult(Array.Empty<byte>());
    }

    private async Task WriteChunkToPartFile(FileStream partFile, byte[] buffer, CancellationToken ct)
    {
        await partFile.WriteAsync(buffer, ct);
    }

    private void VerifyUpload(long receivedSize, long expectedSize)
    {
        if (receivedSize != expectedSize)
        {
            throw new InvalidDataException(
                $"Size mismatch: received {receivedSize}, expected {expectedSize}");
        }
    }

    private void CompleteUpload(string targetPath)
    {
        string partPath = targetPath + ".part";
        File.Move(partPath, targetPath, overwrite: true);
    }

    #endregion

    #region Cleanup

    public void RollbackUpload(string targetPath)
    {
        bool deleted = DeletePartialFile(targetPath);

        Logger.Info($"[Storage] Rollback '{targetPath}', deleted={deleted}");
    }

    public bool DeletePartialFile(string filePath)
    {
        string partPath = filePath.EndsWith(".part", StringComparison.OrdinalIgnoreCase)
            ? filePath
            : filePath + ".part";

        try
        {
            if (!File.Exists(partPath))
            {
                return false;
            }

            File.Delete(partPath);
            return true;
        }
        catch (Exception ex)
        {
            Logger.Warn($"[Storage] Could not delete partial file '{partPath}': {ex.Message}");
            return false;
        }
    }

    #endregion
}

// TODO: chưa dùng tới (dành cho hướng ReceiveFileAsync raw-byte ở trên).
// Nga (Protocol) nên định nghĩa lại field cho đúng thiết kế cuối cùng.
public class UploadChunkHeader
{
    public int ChunkIndex { get; set; }
    public int Length { get; set; }
}
