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

private static string NextAvailableName(string targetPath)
{
   var uploadFolder = Path.GetDirectoryName(targetPath)!;
        var nameOnly = Path.GetFileNameWithoutExtension(targetPath);
        var ext = Path.GetExtension(targetPath);
        int duplicateIndex = 1;
        string candidate;
        do
        {
            candidate = Path.Combine(uploadFolder, $"{nameOnly}({duplicateIndex}){ext}");
            duplicateIndex++;
        } while (File.Exists(candidate) || File.Exists(candidate + ".part"));     
       return candidate;
}

#region Upload Process

    public async Task<string> ReceiveFileAsync(Stream stream, string targetPath, FileStream partFile, long expectedSize, CancellationToken ct, int idleTimeoutMs = 0)
    {
        // TODO: loop reading chunks (header + data) until expectedSize is reached,
        // write to partFile, then VerifyUpload + CompleteUpload,
        // catch OperationCanceledException / TimeoutException / Exception -> RollbackUpload then rethrow
        return await Task.FromResult(string.Empty);
    }

       private async Task<UploadChunkHeader> ReadChunkHeader(Stream stream, CancellationToken ct, int idleTimeoutMs)
    {
        var chunkHeaderJson = await MessageFramer.ReadJsonAsync(stream, ct, idleTimeoutMs);
        return JsonSerializer.Deserialize<UploadChunkHeader>(chunkHeaderJson)
            ?? throw new InvalidDataException("Bad chunk header");
    }

    private async Task<byte[]> ReadChunkData(Stream stream, int chunkLength, CancellationToken ct, int idleTimeoutMs)
     {
        return await MessageFramer.ReadRawAsync(stream, chunkLength, ct, idleTimeoutMs);
    }

    private async Task WriteChunkToPartFile(FileStream partFile, byte[] buffer, CancellationToken ct)
    {
        // TODO: write buffer to partFile
        await Task.CompletedTask;
    }

    private void VerifyUpload(long receivedSize, long expectedSize)
    {
        // TODO: compare receivedSize with expectedSize, throw InvalidDataException if mismatched
    }

    private void CompleteUpload(string targetPath)
    {
        // TODO: rename the .part file to the actual targetPath (File.Move, overwrite: true)
    }

    #endregion

    #region Cleanup

    public void RollbackUpload(string targetPath)
    {
        // TODO: call DeletePartialFile, log if deletion succeeds
    }

    public bool DeletePartialFile(string filePath)
    {
        // TODO: resolve the .part path, check if it exists, delete if present, catch errors if file is locked
        return false;
    }

    #endregion
}