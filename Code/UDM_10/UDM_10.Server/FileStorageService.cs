using System.IO;
using System.Text.Json;
using System.Threading;
using UDM_10.Shared.Config;
using UDM_10.Shared.Models;
using UDM_10.Shared.Protocol;

namespace UDM_10.Server;

public class FileStorageService
{
    private const int MaxFileNameLength = 255;

    // TODO: list Windows reserved file names (CON, PRN, AUX, NUL, COM1-9, LPT1-9)
    private static readonly string[] _windowsReservedNames = { };

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
}