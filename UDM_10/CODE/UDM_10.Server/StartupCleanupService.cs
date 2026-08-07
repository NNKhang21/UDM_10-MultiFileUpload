using System.IO;
using System.Linq;
using UDM_10.Shared.Config;

namespace UDM_10.Server
{
    // NOTE (fix): trước đây các hàm nhận "ServerConfig config" làm tham số, nhưng
    // ServerConfig là static class (không thể truyền làm instance) -> đã đổi các hàm
    // sang dùng thẳng ServerConfig.* (static), bỏ tham số. Cũng gỡ "using UDM_10.Shared.Protocol"
    // vì file này không dùng gì tới Protocol.
    public static class StartupCleanupService
    {
        #region Validation

        public static bool ValidateUploadDirectory()
        {
            try
            {
                if (!Directory.Exists(ServerConfig.UploadDirectory))
                {
                    Directory.CreateDirectory(ServerConfig.UploadDirectory);
                }

                string testFile = Path.Combine(ServerConfig.UploadDirectory, ".write_test");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region Cleanup

        public static int CleanupPartialFiles()
        {
            if (!Directory.Exists(ServerConfig.UploadDirectory)) return 0;

            int count = 0;
            foreach (var file in Directory.GetFiles(ServerConfig.UploadDirectory, "*.part"))
            {
                try
                {
                    File.Delete(file);
                    count++;
                }
                catch (IOException)
                {
                    // File đang bị khoá, bỏ qua thay vì dừng toàn bộ
                }
            }
            return count;
        }

        public static int CleanupEmptyFolders()
        {
            if (!Directory.Exists(ServerConfig.UploadDirectory)) return 0;

            int count = 0;
            var folders = Directory.GetDirectories(ServerConfig.UploadDirectory, "*", SearchOption.AllDirectories)
                .OrderByDescending(f => f.Length);

            foreach (var folder in folders)
            {
                if (!Directory.EnumerateFileSystemEntries(folder).Any())
                {
                    Directory.Delete(folder);
                    count++;
                }
            }
            return count;
        }

        #endregion

        #region Orchestration

        public static (int FilesDeleted, int FoldersDeleted) RunStartupCleanup()
        {
            if (!ValidateUploadDirectory())
            {
                Logger.Error("Không thể tạo/ghi vào thư mục upload.");
                return (0, 0);
            }

            int files = CleanupPartialFiles();
            int folders = CleanupEmptyFolders();
            Logger.Info($"Startup cleanup: {files} file .part, {folders} thư mục rỗng đã xoá.");
            return (files, folders);
        }

        #endregion
    }
}
