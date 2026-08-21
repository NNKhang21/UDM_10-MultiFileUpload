using System;
using System.IO;

namespace UDM_10.Server
{
    // Logger riêng cho UDM_10.Server (Program.cs, ClientSession.cs gọi trực
    // tiếp Logger.Xxx() không qua using -> phải cùng namespace UDM_10.Server).
    //
    // NOTE: khác với UDM_10.Client.Logging.Logger (chỉ có Init/Info/Error),
    // bản này có thêm Warn() vì ClientSession.cs cần dùng.
    public static class Logger
    {
        private static readonly string LogFolder = "logs";
        private static readonly string LogFile = Path.Combine(LogFolder, "server.log");
        private static readonly object _lock = new();

        public static void Init()
        {
            if (!Directory.Exists(LogFolder))
            {
                Directory.CreateDirectory(LogFolder);
            }

            if (!File.Exists(LogFile))
            {
                File.Create(LogFile).Close();
            }
        }

        public static void Info(string message) => WriteLog("INFO", message);

        public static void Warn(string message) => WriteLog("WARN", message);

        public static void Error(string message) => WriteLog("ERROR", message);

        private static void WriteLog(string level, string message)
        {
            string log = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";

            // Console.WriteLine + File.AppendAllText có thể bị gọi đồng thời
            // từ nhiều ClientSession -> khoá lại để tránh log lẫn dòng nhau.
            lock (_lock)
            {
                Console.WriteLine(log);
                File.AppendAllText(LogFile, log + Environment.NewLine);
            }
        }
    }
}
