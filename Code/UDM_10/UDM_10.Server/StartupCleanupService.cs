using System;
using System.IO;
using UDM_10.Shared.Config;
using UDM_10.Shared.Protocol;

namespace UDM_10.Server;
// Don rac ".part" con sot lai tu lan chay truoc, chay luc Server khoi dong
public static class StartupCleanupService
{
     // Check thu muc upload co ton tai va ghi duoc khong, tu tao neu chua co
    public static bool ValidateUploadDirectory(ServerConfig config)
    {
        if (!Directory.Exists(config.UploadDirectory))
        {
            try
            {
                Directory.CreateDirectory(config.UploadDirectory);
                Logger.Info(ServerEvent.Cleanup, "Upload directory created", ("path", config.UploadDirectory));
            }
            catch (Exception ex)
{
    if (ex is IOException || ex is UnauthorizedAccessException)
    {
        Logger.Warn(ServerEvent.Cleanup, "Could not create upload directory", ("path", config.UploadDirectory), ("error", ex.Message));
        return false;
    }

    throw;
}
        }
        string testFile = Path.Combine(config.UploadDirectory, ".writetest");
        try
        {
            File.WriteAllText(testFile, "");
            File.Delete(testFile);
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            Logger.Warn(ServerEvent.Cleanup, "Upload directory not writable",("path", config.UploadDirectory), ("error", ex.Message));
            return false;
        }
        return true;
    }

    // Quet va xoa toan bo file .part mo coi, gap file bi khoa thi bo qua chu khong dung lai
    public static int CleanupPartialFiles(ServerConfig config)
    {
        if (!Directory.Exists(config.UploadDirectory)) return 0;
        string[] partFiles = Directory.GetFiles(config.UploadDirectory, "*.part");
        int deletedCount = 0;

        foreach (string partPath in partFiles)
        {
            try
            {
                File.Delete(partPath);
                deletedCount++;
                Logger.Info(ServerEvent.Cleanup, "Cleanup success", ("path", partPath));
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                // File dang bi khoa, bo qua va xu ly file tiep theo
                Logger.Warn(ServerEvent.Cleanup, "Cleanup fail", ("path", partPath), ("error", ex.Message));
            }
        }
        return deletedCount;
    }

    public static int CleanupEmptyFolders(ServerConfig config)
    {
       // TODO: if UploadDirectory does not exist, return 0
        // TODO: get all subfolders (AllDirectories), sort by path length descending
        // (delete child folders before parent folders), delete empty ones, count how many were deleted
        return 0;
    }

  

    #region Orchestration

    public static (int FilesDeleted, int FoldersDeleted) RunStartupCleanup(ServerConfig config)
    {
        // TODO: call ValidateUploadDirectory first, if it fails return (0,0) and log an error,
        // otherwise call CleanupPartialFiles then CleanupEmptyFolders, log the result, return the tuple
        return (0, 0);
    }

    #endregion
}