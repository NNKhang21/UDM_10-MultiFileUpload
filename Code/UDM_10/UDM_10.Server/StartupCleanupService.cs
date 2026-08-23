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

     // Xoa cac thu muc con rong ben trong thu muc upload
    public static int CleanupEmptyFolders(ServerConfig config)
    {
        if (!Directory.Exists(config.UploadDirectory)) return 0;

        string[] allFolders = Directory.GetDirectories(config.UploadDirectory,"*",SearchOption.AllDirectories);
        for (int i = 0; i < allFolders.Length - 1; i++)
{
    for (int j = i + 1; j < allFolders.Length; j++)
    {
        if (allFolders[i].Length < allFolders[j].Length)
        {
            string temp = allFolders[i];
            allFolders[i] = allFolders[j];
            allFolders[j] = temp;
        }
    }
}
        int deletedCount = 0;
        foreach (string folder in allFolders)
        {
            if (Directory.GetFileSystemEntries(folder).Length != 0)
                continue;
            try
            {
                Directory.Delete(folder);
                deletedCount++;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                Logger.Warn(ServerEvent.Cleanup, "Could not delete folder", ("path", folder), ("error", ex.Message));
            }
        }

        return deletedCount;
    }
   /// Chay cac buoc don rac o tren, goi 1 lan luc Server khoi dong.
    public static (int FilesDeleted, int FoldersDeleted) RunStartupCleanup(ServerConfig config)
    {
        if (!ValidateUploadDirectory(config))
        {
            Logger.Error(ServerEvent.Cleanup, "Upload directory invalid, cannot start server", ("path", config.UploadDirectory));
            throw new InvalidOperationException($"Upload directory '{config.UploadDirectory}' is invalid or not writable.");
        }
        int filesDeleted = CleanupPartialFiles(config);
        int foldersDeleted = CleanupEmptyFolders(config);
        Logger.Info(ServerEvent.Cleanup, $"Startup cleanup done: {filesDeleted} file(s), {foldersDeleted} folder(s) removed");
        return (filesDeleted, foldersDeleted);
    }
}