using System;
using System.IO;

namespace UDM_10.Server
{
    // NOTE (fix): namespace trước là UDM_10.Client.Server -> không khớp với các file
    // khác trong Server (UDM_10.Server) -> đã sửa lại cho thống nhất.
    public static class Logger
    {
        private static readonly string LogFolder = "logs";
        private static readonly string LogFile = Path.Combine(LogFolder, "server.log");

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

        public static void Info(string message)
        {
            WriteLog("INFO", message);
        }

        public static void Error(string message)
        {
            WriteLog("ERROR", message);
        }

        private static void WriteLog(string level, string message)
        {
            string log = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";

            Console.WriteLine(log);

            File.AppendAllText(LogFile, log + Environment.NewLine);
        }
    }
}
