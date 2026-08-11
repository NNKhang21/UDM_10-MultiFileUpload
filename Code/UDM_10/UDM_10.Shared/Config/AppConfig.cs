using System.IO;
using System.Text.Json;

namespace UDM_10.Shared.Config
{
    public static class ServerConfig
    {
        public static string IP { get; private set; } = string.Empty;
        public static int Port { get; private set; }
        public static int Timeout { get; private set; }

       
        public static string UploadDirectory { get; private set; } = "uploads";
        public static long MaxFileSizeMb { get; private set; } = 500;

        public static void Load()
        {
            using JsonDocument doc = LoadDocument();
            JsonElement server = doc.RootElement.GetProperty("Server");

            IP = server.TryGetProperty("IP", out var ip) ? ip.GetString() ?? string.Empty : string.Empty;
            Port = server.TryGetProperty("Port", out var port) ? port.GetInt32() : 5000;
            Timeout = server.TryGetProperty("Timeout", out var timeout) ? timeout.GetInt32() : 30000;
            UploadDirectory = server.TryGetProperty("UploadDirectory", out var dir) ? dir.GetString() ?? "uploads" : "uploads";
            MaxFileSizeMb = server.TryGetProperty("MaxFileSizeMb", out var max) ? max.GetInt64() : 500;
        }

        private static JsonDocument LoadDocument()
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
            string json = File.ReadAllText(path);
            return JsonDocument.Parse(json);
        }
    }

    public static class ClientConfig
    {
        public static string ServerIP { get; private set; } = string.Empty;
        public static int Port { get; private set; }
        public static int Timeout { get; private set; }

        public static void Load()
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
            string json = File.ReadAllText(path);
            using JsonDocument doc = JsonDocument.Parse(json);
            JsonElement client = doc.RootElement.GetProperty("Client");

            ServerIP = client.TryGetProperty("ServerIP", out var ip) ? ip.GetString() ?? string.Empty : string.Empty;
            Port = client.TryGetProperty("Port", out var port) ? port.GetInt32() : 5000;
            Timeout = client.TryGetProperty("Timeout", out var timeout) ? timeout.GetInt32() : 30000;
        }
    }
}
