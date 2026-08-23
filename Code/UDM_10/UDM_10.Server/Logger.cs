namespace UDM_10.Server;

using System.IO;

public static class Logger
{
    private static readonly object _lock = new();
    private static string _logFilePath = "logs/server.log";

    public static void Init(string logFilePath)
    {
        _logFilePath = logFilePath;
        var dir = Path.GetDirectoryName(_logFilePath);

        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
    }

    // OVERLOAD ĐỂ CÁC FILE KHÁC CÓ THỂ GỌI ĐƠN GIẢN 
    public static void Init()
    {
        Init("logs/server.log");
    }

    public static void Info(string message)
    {
        Info("GENERAL", message);
    }

    public static void Warn(string message)
    {
        Warn("GENERAL", message);
    }

    public static void Error(string message)
    {
        Error("GENERAL", message);
    }

    //CÁC HÀM LOG

    public static void Info(string evt, string message,
        params (string key, object value)[] fields)
        => Write("INFO", evt, message, fields);

    public static void Warn(string evt, string message,
        params (string key, object value)[] fields)
        => Write("WARN", evt, message, fields);

    public static void Error(string evt, string message,
        params (string key, object value)[] fields)
        => Write("ERROR", evt, message, fields);

    private static void Write(string level, string evt, string message,
        (string key, object value)[] fields)
    {
        var kv = string.Join(" ",
            fields.Select(f => $"{f.key}={f.value}"));

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