using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using UDM_10.Shared.Config;
using UDM_10.Shared.Models;
using UDM_10.Shared.Protocol;
namespace UDM_10.Server;
// Week 1 skeleton - FileStorageService (logic not implemented yet)
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
        // TODO: check empty, too long, invalid characters, path traversal, reserved name
    }

    public void ValidateFileSize(long fileSize)
    {
        // TODO: check fileSize <= 0 or exceeds config's MaxFileSizeMb
    }

    public void ValidateChunkLength(long declaredLength, long remainingBytes)
    {
        // TODO: check declaredLength <= 0 or greater than remainingBytes
    }

    #endregion

    #region Upload Preparation

    public string GetUploadPath(string fileName)
    {
        // TODO: combine UploadDirectory + fileName, check for name conflicts (including .part files),
        // handle according to DuplicatePolicy: Reject / Overwrite / Rename
        return string.Empty;
    }

    public async Task<(string TargetPath, FileStream PartFile)> PrepareUploadAsync(string fileName, CancellationToken ct = default)
    {
        // TODO: lock with _nameLock, call GetUploadPath, create a new ".part" FileStream (CreateNew)
        return await Task.FromResult((string.Empty, (FileStream)null));
    }

    private static string GenerateDuplicateName(string targetPath)
    {
        // TODO: loop to generate name(1), name(2)... until no conflict with file/.part
        return string.Empty;
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
        // TODO: read JSON header from stream via MessageFramer, deserialize into UploadChunkHeader
        return await Task.FromResult<UploadChunkHeader>(null);
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