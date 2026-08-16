namespace UDM_10.Server;

using System.IO;
// [B] Owner: thanh vien phu trach Shared - Config & Log
// Format: {timestamp} [{level}] [{event}] {message} | key=value ...
// Khong dung thu vien ngoai (Serilog) de tranh phu thuoc NuGet - de nhom tu build offline neu can.
public static class Logger
{
    private static readonly object _lock = new();
    private static string _logFilePath = "logs/server.log";

    public static void Init(string logFilePath)
    {
        _logFilePath = logFilePath;
        var dir = Path.GetDirectoryName(_logFilePath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
    }


    // Ghi log mức INFO
    public static void Info(
        string evt,
        string message,
        params (string key, object value)[] fields)
    {
        Write("INFO", evt, message, fields);
    }

    // Hàm ghi log nội bộ
    private static void Write(
        string level,
        string evt,
        string message,
        (string key, object value)[] fields)
    {
        var kv = string.Join(
            " ",
            fields.Select(f => $"{f.key}={f.value}")
        );

        var line =
            $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} " +
            $"[{level}] [{evt}] {message}" +
            (kv.Length > 0 ? $" | {kv}" : "");

        lock (_lock)
        {
            Console.WriteLine(line);
            File.AppendAllText(
                _logFilePath,
                line + Environment.NewLine
            );
        }
    }
}