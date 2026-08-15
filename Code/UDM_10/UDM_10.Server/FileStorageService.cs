using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using UDM_10.Shared.Config;
using UDM_10.Shared.Models;
using UDM_10.Shared.Protocol;
namespace UDM_10.Server;
// Week 3 - Duplicate handling & upload target reservation
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

    public FileStorageService(ServerConfig config)
    {
        _config = config;
        Directory.CreateDirectory(_config.UploadDirectory);
    }

    #region Validation

     public void ValidateFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            Logger.Warn(ServerEvent.ValidationFailed, "Empty filename", ("fileName", fileName ?? ""));
            throw new ArgumentException("Empty filename");
        }

        if (fileName.Length > MaxFileNameLength)
        {
            Logger.Warn(ServerEvent.ValidationFailed, "Filename too long",
                ("fileName", fileName), ("length", fileName.Length));
            throw new ArgumentException("Filename too long");
        }

        if (fileName.Contains("..") || Path.IsPathRooted(fileName))
        {
            Logger.Warn(ServerEvent.ValidationFailed, "Invalid filename (path traversal)", ("fileName", fileName));
            throw new ArgumentException("Invalid filename (path traversal)");
        }

        if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            Logger.Warn(ServerEvent.ValidationFailed, "Invalid characters in filename", ("fileName", fileName));
            throw new ArgumentException("Invalid characters in filename");
        }

  var nameOnly = Path.GetFileNameWithoutExtension(fileName).ToUpperInvariant();
        for (int i = 0; i < _windowsReservedNames.Length; i++)
        {
            if (nameOnly == _windowsReservedNames[i])
            {
                Logger.Warn(ServerEvent.ValidationFailed, "Reserved filename", ("fileName", fileName));
                throw new ArgumentException("Reserved filename");
            }
        }
    }

     public void ValidateFileSize(long fileSize)
    {
        long maxBytes = (long)_config.MaxFileSizeMb * 1024 * 1024;
        if (fileSize <= 0 || fileSize > maxBytes)
        {
            Logger.Warn(ServerEvent.ValidationFailed, "Invalid file size",
                ("fileSize", fileSize), ("maxBytes", maxBytes));
            throw new ArgumentException($"Size {fileSize} exceeds limit {maxBytes} bytes");
        }
    }
    public void ValidateChunkLength(long declaredLength, long remainingBytes)
    {
        // TODO: check declaredLength <= 0 or greater than remainingBytes
    }

    #endregion

    #region Upload Preparation

 
        // TODO: ccheck for name conflicts (including .part files),
        // handle according to DuplicatePolicy: Reject / Overwrite / Rename
        public string ResolveFinalPath(string fileName)
    {
    var targetPath = Path.Combine(_config.UploadDirectory, fileName);

    if (!File.Exists(targetPath) && !File.Exists(targetPath + ".part"))
    {
        return targetPath;
    }

    Logger.Info(
        ServerEvent.UploadStart,
        "Duplicate filename detected", 
        ("fileName", fileName), ("policy", _config.DuplicatePolicy));
    if (_config.DuplicatePolicy == "Reject")
    {
        Logger.Warn(
            ServerEvent.ValidationFailed, "File already exists, upload rejected", ("fileName", fileName));
        throw new IOException($"File already exists: {fileName}");
    }
    if (_config.DuplicatePolicy == "Overwrite")
 {
    return targetPath;
 }
    return NextAvailableName(targetPath);
  }
    

public async Task<(string TargetPath, FileStream PartFile)> ReserveUploadTargetAsync(string fileName, CancellationToken ct = default)
  { await _nameLock.WaitAsync(ct);
    try
    {
        var targetPath = ResolveFinalPath(fileName);
        var partFile = new FileStream( targetPath + ".part",FileMode.CreateNew, FileAccess.Write);
        return (targetPath, partFile);
    }
    finally
    {
        _nameLock.Release();
    }
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
#endregion
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
        // TODO: read raw data from stream via MessageFramer according to chunkLength
        return await Task.FromResult(Array.Empty<byte>());
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