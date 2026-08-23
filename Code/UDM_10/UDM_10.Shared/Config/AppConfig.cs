using System.IO;
using System.Text.Json;

namespace UDM_10.Shared.Config;

public class ServerConfig
{
    public string Host { get; set; } = "0.0.0.0";
    public int Port { get; set; } = 9000;
    public string UploadDirectory { get; set; } = "./uploads";
    public int MaxFileSizeMb { get; set; } = 150;


    // Dung de kiem tra kich thuoc moi chunk upload tu Client
    public int ChunkSizeKb { get; set; } = 64;
    public string DuplicatePolicy { get; set; } = "Rename"; // Overwrite | Rename | Reject

    public int IdleTimeoutSeconds { get; set; } = 30;

    public static ServerConfig Load(string path)
    {
        try
        {
            if (!File.Exists(path))
                return new ServerConfig();

            var json = File.ReadAllText(path);

            return JsonSerializer.Deserialize<ServerConfig>(
                json,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }
            ) ?? new ServerConfig();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WARN] Khong the doc ServerConfig: {ex.Message}");
            return new ServerConfig();
        }
    }
}
    // Gia tri mac dinh MaxConcurrentUploads = 5: tham khao chuan thuc te - trinh duyet Chrome/
    // Firefox gioi han 6 ket noi dong thoi/host, cac thu vien upload pho bien (Uppy, Dropzone.js,
    // Fine Uploader) mac dinh 3-5 file song song. Chon 5 vi Server o day la TcpListener don gian
    // (1 Task/ket noi, ghi truc tiep file .part xuong dia, khong co thread-pool/queue chuyen dung)
    // - can du cao de demo thay ro nhieu progress bar chay song song, nhung khong qua cao de
    // tranh tranh chap I/O dia khi nhieu file cung ghi 1 luc tren 1 may test.
    public class ClientConfig
    {
        public string DefaultServerIp { get; set; } = "127.0.0.1";
        public int DefaultPort { get; set; } = 9000;
        public int MaxConcurrentUploads { get; set; } = 5;
        public int MaxFiles { get; set; } = 50;
        public int MaxFileSizeMb { get; set; } = 100;
        public int ChunkSizeKb { get; set; } = 64;
        public int ConnectTimeoutSeconds { get; set; } = 10;

        // BUGFIX #TIMEOUT: tuong tu ServerConfig.IdleTimeoutSeconds - thoi gian toi da Client
        // cho phan hoi (UploadStartAck/UploadResult) tu Server truoc khi bao loi ro rang thay
        // vi treo vo han neu Server bi "dong" (process con song nhung khong xu ly).
        public int IdleTimeoutSeconds { get; set; } = 30;

        public static ClientConfig Load(string path)
        {
            try
            {
                if (!File.Exists(path))
                    return new ClientConfig();

                var json = File.ReadAllText(path);

                return JsonSerializer.Deserialize<ClientConfig>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }
                ) ?? new ClientConfig();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARN] Khong the doc ClientConfig: {ex.Message}");
                return new ClientConfig();
            }
        }
    }
