using System.IO;
using UDM_10.Shared.Config;
using UDM_10.Shared.Protocol;

namespace UDM_10.Server;

public static class StartupCleanupService
{
    #region Validation

    public static bool ValidateUploadDirectory(ServerConfig config)
    {
        // TODO: check if upload directory exists, create it if not,
        // then write a test file and delete it to make sure the directory is writable
        return false;
    }

    #endregion

    #region Cleanup

    public static int CleanupPartialFiles(ServerConfig config)
    {
        // TODO: scan all "*.part" files in UploadDirectory, delete each one,
        // skip locked files instead of stopping, count how many files were deleted
        return 0;
    }

    public static int CleanupEmptyFolders(ServerConfig config)
    {
        // TODO: get all subfolders (AllDirectories), sort by path length descending
        // (delete child folders before parent folders), delete empty ones, count how many were deleted
        return 0;
    }

    #endregion

    #region Orchestration

    public static (int FilesDeleted, int FoldersDeleted) RunStartupCleanup(ServerConfig config)
    {
        // TODO: call ValidateUploadDirectory first, if it fails return (0,0) and log an error,
        // otherwise call CleanupPartialFiles then CleanupEmptyFolders, log the result, return the tuple
        return (0, 0);
    }

    #endregion
}