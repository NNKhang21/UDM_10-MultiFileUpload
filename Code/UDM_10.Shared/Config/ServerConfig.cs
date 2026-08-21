using System;
using System.IO;
using System.Text.Json;

namespace UDM_10.Shared.Config
{
    // Cấu hình cho Server, đọc từ appsettings.json.
    //
    // NOTE: Khác với UDM_10.Client.Shared.Config.ServerConfig (class static,
    // dùng Microsoft.Extensions.Configuration cho Client) - đây là bản dành
    // riêng cho UDM_10.Server, trả về instance để Program.cs / FileStorageService
    // / StartupCleanupService dùng chung qua tham số truyền vào.
    public class ServerConfig
    {
        public string Host { get; set; } = "0.0.0.0";

        public int Port { get; set; } = 5000;

        public int IdleTimeoutSeconds { get; set; } = 30;

        public string UploadDirectory { get; set; } = "uploads";

        public int MaxFileSizeMb { get; set; } = 1024;

        public static ServerConfig Load(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    return new ServerConfig();
                }

                string json = File.ReadAllText(path);

                using JsonDocument doc = JsonDocument.Parse(json);

                if (!doc.RootElement.TryGetProperty("Server", out JsonElement section))
                {
                    return new ServerConfig();
                }

                var config = JsonSerializer.Deserialize<ServerConfig>(
                    section.GetRawText(),
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                return config ?? new ServerConfig();
            }
            catch (Exception)
            {
                // Cấu hình lỗi/không tồn tại -> dùng giá trị mặc định,
                // Program.cs sẽ log lại thông tin đã load.
                return new ServerConfig();
            }
        }
    }
}
