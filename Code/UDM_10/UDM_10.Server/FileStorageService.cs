using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using UDM_10.Shared.Config;
using UDM_10.Shared.Models;
using UDM_10.Shared.Protocol;
namespace UDM_10.Server;
public class FileStorageService
{
    private const int MaxFileNameLength = 255;

private static readonly string[] _windowsReservedNames =
{
    "CON","PRN","AUX","NUL",  "COM1",  "COM2","COM3","COM4", "COM5","COM6", "COM7", "COM8", "COM9", 
    "LPT1",  "LPT2",  "LPT3", "LPT4",  "LPT5",  "LPT6", "LPT7", "LPT8", "LPT9"
};
      private readonly ServerConfig _config;
    // Khoa de dung chung Dictionary _uploads cho an toan
    private readonly object _lock = new object();
    // Danh sach cac upload dang chay, key la TransferId
    private readonly Dictionary<string, UploadContext> _uploads = new Dictionary<string, UploadContext>(StringComparer.OrdinalIgnoreCase);
    private class UploadContext
    {
        public string TransferId { get; set; } = "";
        public string FileName { get; set; } = "";
        public string TargetPath { get; set; } = "";
        public FileStream PartFile { get; set; } = null!;
        public long ExpectedSize { get; set; }
        public long ReceivedSize { get; set; }
        public int NextChunkIndex { get; set; }
        public bool OverwriteOnFinish { get; set; }
        // Rieng cho 1 upload, dung SemaphoreSlim vi phai await luc ghi file 
        public SemaphoreSlim Lock { get; } = new SemaphoreSlim(1, 1);
    }
      public FileStorageService(ServerConfig config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        Directory.CreateDirectory(_config.UploadDirectory);
    }
       // Kiem tra ten file: rong, dai qua, path traversal, ky tu la, ket thuc bang dot/space, ten cam cua Windows.
    public void ValidateFileName(string fileName)
    {
    if (string.IsNullOrWhiteSpace(fileName)) 
    throw new ArgumentException("Empty filename", nameof(fileName));
    if (fileName.Length > MaxFileNameLength) 
    throw new ArgumentException("Filename too long", nameof(fileName));
        // Chan rieng '/' va '\': "a/b" khong co ".." nhung van la traversal
     if (fileName.Contains(".."))
    throw new ArgumentException("Invalid filename (path traversal)", nameof(fileName));

    if (fileName.Contains('/') || fileName.Contains('\\'))
    throw new ArgumentException("Invalid filename (path traversal)", nameof(fileName));
    if (Path.IsPathRooted(fileName))
    throw new ArgumentException("Invalid filename (path traversal)", nameof(fileName));
    if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
         throw new ArgumentException("Invalid filename characters", nameof(fileName));
    if (fileName.EndsWith('.') || fileName.EndsWith(' ')) 
        throw new ArgumentException("Filename cannot end with dot or space", nameof(fileName));
        // Lay phan ten truoc dau cham dau tien de so voi danh sach cam (vd "CON.txt" cung bi cam)
        string nameOnly = fileName.TrimEnd('.', ' ').Split('.')[0].ToUpperInvariant();
     foreach (string reserved in _windowsReservedNames)
        if (nameOnly == reserved) throw new ArgumentException("Reserved filename", nameof(fileName));
    }
       public void ValidateFileSize(long fileSize)
    {
        long maxBytes = (long)_config.MaxFileSizeMb * 1024 * 1024;
        if (fileSize <= 0 || fileSize > maxBytes)
            throw new ArgumentException($"Invalid file size: {fileSize} (max {maxBytes} bytes)");
    }
    // declaredLength khong duoc vuot qua phan con lai hoac ChunkSizeKb da cau hinh
    public void ValidateChunkLength(long declaredLength, long remainingBytes)
    {
        long maxChunkBytes = (long)_config.ChunkSizeKb * 1024;
        if (maxChunkBytes <= 0 || declaredLength <= 0 || declaredLength > remainingBytes || declaredLength > maxChunkBytes)
            throw new InvalidDataException($"Invalid chunk length {declaredLength} (remaining {remainingBytes})");
    }
    private static void ValidateTransferId(string transferId)
    {
        if (string.IsNullOrWhiteSpace(transferId)) 
        throw new ArgumentException("Invalid transfer id", nameof(transferId));
    }

    // DUONG DAN FILE
    public string ResolveFinalPath(string fileName)
    {
        ValidateFileName(fileName);
        string targetPath = Path.Combine(_config.UploadDirectory, fileName);
        string policy = NormalizePolicy();
        // File ".part" cung tinh la ten da bi chiem
        if (!File.Exists(targetPath) && !File.Exists(targetPath + ".part")) return targetPath;
        if (policy == "REJECT") 
        throw new IOException($"File already exists: {fileName}");
        if (policy == "OVERWRITE") return targetPath;
        return NextAvailableName(targetPath); // con lai la RENAME
    }
    private string NormalizePolicy()
    {
        string policy = (_config.DuplicatePolicy ?? "").Trim().ToUpperInvariant();
        if (policy != "REJECT" && policy != "OVERWRITE" && policy != "RENAME")
            throw new InvalidOperationException($"Invalid DuplicatePolicy: {_config.DuplicatePolicy}");
        return policy;
    }

     // Tim ten dang "ten(1).ext", "ten(2).ext"... chua bi chiem, thu toi da 1000 lan
    private static string NextAvailableName(string targetPath)
    {
        string folder = Path.GetDirectoryName(targetPath)!;
        string name = Path.GetFileNameWithoutExtension(targetPath);
        string ext = Path.GetExtension(targetPath);
        for (int i = 1; i <= 1000; i++)
        {
            string candidate = Path.Combine(folder, $"{name}({i}){ext}");
            if (!File.Exists(candidate) && !File.Exists(candidate + ".part")) return candidate;
        }
        throw new IOException("Too many duplicate filename attempts");
    }
      // ---- BAT DAU UPLOAD ----
    // Kiem tra du lieu, tao file ".part" va luu UploadContext theo TransferId.
    // Tao file la I/O nen lam NGOAI lock, chi lock luc doc/ghi Dictionary de khong giu lock qua lau.
    public Task BeginUploadAsync(UploadStartMessage start, CancellationToken ct = default)
    {
        if (start == null) throw new ArgumentNullException(nameof(start));
        ct.ThrowIfCancellationRequested();
        ValidateTransferId(start.TransferId);
        ValidateFileName(start.FileName);
        ValidateFileSize(start.FileSize);

        lock (_lock)
        {
            if (_uploads.ContainsKey(start.TransferId))
                throw new InvalidOperationException($"Upload already exists: {start.TransferId}");
        }

        string policy = NormalizePolicy();
        string targetPath;
        string partPath;
        FileStream partFile;
        while (true)
        {
            targetPath = ResolveFinalPath(start.FileName);
            partPath = targetPath + ".part";
            try
            {
                partFile = new FileStream(partPath, FileMode.CreateNew, FileAccess.ReadWrite,
                    FileShare.None, 64 * 1024, FileOptions.Asynchronous);
                break;
            }
            catch (IOException)
            {
                if (!File.Exists(partPath)) throw; // loi khac, khong phai do trung ten
                if (policy == "RENAME") continue; // trung ten, thu ten khac
                throw new InvalidOperationException($"File is being uploaded: {start.FileName}");
            }
        }

      bool duplicateTransfer = false;

    lock (_lock)
    {
    // Kiem tra TransferId lan nua sau khi tao file
    if (_uploads.ContainsKey(start.TransferId))
    {
        duplicateTransfer = true;
    }
    else
    {
        _uploads.Add(start.TransferId, new UploadContext
        {
            TransferId = start.TransferId,
            FileName = start.FileName,
            TargetPath = targetPath,
            PartFile = partFile,
            ExpectedSize = start.FileSize,
            OverwriteOnFinish = policy == "OVERWRITE"
        });
        }
    }

        if (duplicateTransfer)
    {
    try { partFile.Dispose(); } catch { }
    DeletePartialFile(targetPath);

    throw new InvalidOperationException(
        $"Upload already exists: {start.TransferId}");
        }

        Logger.Info(ServerEvent.UploadStart, "Upload begin", ("fileName", (object)start.FileName), ("transferId", (object)start.TransferId), ("expectedSize", (object)start.FileSize));
        return Task.CompletedTask;
    }
    // ---- GHI CHUNK ----
    // Giai ma base64, kiem tra thu tu/do dai roi ghi vao file ".part". Loi thi rollback va nem lai loi.
    public async Task WriteChunkAsync(UploadChunkMessage chunk, CancellationToken ct = default)
    {
        if (chunk == null) throw new ArgumentNullException(nameof(chunk));
        ValidateTransferId(chunk.TransferId);
        UploadContext context = GetContext(chunk.TransferId);
        bool locked = false;
        try
        {
            await context.Lock.WaitAsync(ct);
            locked = true;
            if (chunk.ChunkIndex != context.NextChunkIndex)
            throw new InvalidDataException($"Invalid chunk order. Expected={context.NextChunkIndex}, Received={chunk.ChunkIndex}");
            byte[] buffer;
            try
            {
                buffer = Convert.FromBase64String(chunk.DataBase64);
            }
            catch (FormatException) { throw new InvalidDataException("Invalid Base64 data"); }

            if (buffer.Length != chunk.Length) throw new InvalidDataException("Chunk length mismatch");
            ValidateChunkLength(buffer.Length, context.ExpectedSize - context.ReceivedSize);
            await context.PartFile.WriteAsync(buffer, ct);
            context.ReceivedSize += buffer.Length;
            context.NextChunkIndex++;
            Logger.Info(ServerEvent.UploadChunk, "Chunk received", (key: "transferId", value: (object)context.TransferId), (key: "chunkIndex", value: (object)chunk.ChunkIndex), (key: "size", value: (object)buffer.Length));
        }
        catch (OperationCanceledException)
        {
            RollbackUpload(context);
            throw;
        }
        catch (Exception ex)
        {
            Logger.Error(ServerEvent.UploadIncomplete, "Upload failed", (key: "transferId", value: (object)context.TransferId), (key: "fileName", value: (object)context.FileName), (key: "error", value: (object)ex.Message));
            RollbackUpload(context);
            throw;
        }
        finally
        {
            if (locked) context.Lock.Release();
        }
    }

    // ---- KET THUC UPLOAD ----
    // Chi doi ten ".part" thanh ten that khi da nhan DU dung luong. Loi thi tra ve false, khong throw.
    public async Task<(bool Success, string FinalFileName)> FinishUploadAsync(
    UploadDoneMessage done,
    CancellationToken ct = default)
{
    if (done == null) throw new ArgumentNullException(nameof(done));
    ValidateTransferId(done.TransferId);

    UploadContext context = GetContext(done.TransferId);
    bool locked = false;
    try
    {
        await context.Lock.WaitAsync(ct);
        locked = true;
        if (!string.Equals(done.FileName, context.FileName, StringComparison.Ordinal))
            throw new InvalidDataException("UploadDone filename mismatch");
        if (context.ReceivedSize != context.ExpectedSize)
            throw new InvalidDataException("Incomplete upload");

        await context.PartFile.FlushAsync(ct);
        context.PartFile.Dispose();

        try
        {
            File.Move(
                context.TargetPath + ".part",
                context.TargetPath,
                context.OverwriteOnFinish);
        }
        catch (Exception ex)
        {
            if (!(ex is IOException) && !(ex is UnauthorizedAccessException))
                throw;
            RemoveContext(context.TransferId);
            DeletePartialFile(context.TargetPath);
            Logger.Error(ServerEvent.UploadIncomplete, "Final file move failed; upload rolled back", (key: "transferId", value: (object)context.TransferId), (key: "fileName", value: (object)context.FileName), (key: "error", value: (object)ex.Message));

                return (false, string.Empty);
        }
        string finalFileName = Path.GetFileName(context.TargetPath);
        RemoveContext(context.TransferId);

            Logger.Info(ServerEvent.UploadComplete, "Upload finish", (key: "transferId", value: (object)context.TransferId), (key: "fileName", value: (object)finalFileName), (key: "size", value: (object)context.ReceivedSize));
            return (true, finalFileName);
    }
    catch (InvalidDataException ex)
    {
            Logger.Error(ServerEvent.UploadIncomplete, "Upload validation failed", (key: "transferId", value: (object)context.TransferId), (key: "fileName", value: (object)context.FileName), (key: "error", value: (object)ex.Message));
            RollbackUpload(context);
        return (false, string.Empty);
    }
    catch (OperationCanceledException)
    {
        RollbackUpload(context);
        throw;
    }
    catch (Exception ex)
    {
            Logger.Error(ServerEvent.UploadIncomplete, "Upload failed", (key: "transferId", value: (object)context.TransferId), (key: "fileName", value: (object)context.FileName), (key: "error", value: (object)ex.Message));
            RollbackUpload(context);
        throw;
    }
    finally
    {
        if (locked)
            context.Lock.Release();
    }
}
    // ---- HUY, DON DEP ----
    // Cho ClientSession chu dong huy 1 upload dang do dang, vi du khi Client mat ket noi.
    public bool AbortUpload(string transferId)
    {
        ValidateTransferId(transferId);
        UploadContext? context;
        lock (_lock)
        {
            if (!_uploads.TryGetValue(transferId, out context)) return false;
        }
        context.Lock.Wait();
        try
        {
            lock (_lock)
            {
                if (!_uploads.TryGetValue(transferId, out UploadContext? current) ||
                    !ReferenceEquals(current, context)) return false;
                _uploads.Remove(transferId);
            }
            try { context.PartFile.Dispose(); } catch { }
            DeletePartialFile(context.TargetPath);
            Logger.Warn(ServerEvent.UploadIncomplete, "Upload aborted", (key: "transferId", value: (object)transferId), (key: "fileName", value: (object)context.FileName));
            return true;
        }
        finally
        {
            context.Lock.Release();
        }
    }

    private UploadContext GetContext(string transferId)
    {
        lock (_lock)
        {
            if (!_uploads.TryGetValue(transferId, out UploadContext? context))
                throw new InvalidOperationException($"Upload not found: {transferId}");
            return context;
        }
    }

    private void RemoveContext(string transferId)
    {
        lock (_lock)
        {
            _uploads.Remove(transferId);
        }
    }
    // Bo khoi _uploads, dong file, xoa ".part". Kiem tra ReferenceEquals de khong xoa nham
    // context moi neu TransferId nay da duoc dung lai.
    private void RollbackUpload(UploadContext context)
    {
       bool removed = false;

        lock (_lock)
        {
       if (_uploads.TryGetValue(context.TransferId, out UploadContext? current))
         {
        if (ReferenceEquals(current, context))
        {
            _uploads.Remove(context.TransferId);
            removed = true;
        }
    }
    }
        if (!removed) return;
        try { context.PartFile.Dispose(); } catch { }
        DeletePartialFile(context.TargetPath);
        Logger.Warn(ServerEvent.Cleanup, "Upload rollback", (key: "transferId", value: (object)context.TransferId), (key: "fileName", value: (object)context.FileName));
    }

    // Xoa file ".part", dung chung cho cac ham rollback/abort o tren.
    private bool DeletePartialFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return false;
        string partPath = filePath.EndsWith(".part", StringComparison.OrdinalIgnoreCase) ? filePath : filePath + ".part";
        if (!File.Exists(partPath)) return false;
        try
        {
            File.Delete(partPath);
            return true;
        }
        catch (Exception ex)
        {
            if (!(ex is IOException) && !(ex is UnauthorizedAccessException)) throw;
            Logger.Warn(ServerEvent.Cleanup, "Could not remove partial file", (key: "fileName", value: (object)Path.GetFileName(partPath)), (key: "error", value: (object)ex.Message));
            return false;
        }
    }
}
