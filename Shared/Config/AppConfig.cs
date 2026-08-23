using Microsoft.Extensions.Configuration;
using System.IO;

namespace UDM_10.Client.Shared.Config
{
    public static class ServerConfig
    {
        public static string IP { get; private set; } = string.Empty;
        public static int Port { get; private set; }
        public static int Timeout { get; private set; }

        public static void Load()
        {
            IConfiguration config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            IP = config["Server:IP"] ?? string.Empty;
            Port = int.Parse(config["Server:Port"] ?? "5000");
            Timeout = int.Parse(config["Server:Timeout"] ?? "30000");
        }
    }

    public static class ClientConfig
{
    public static string ServerIP { get; private set; } = string.Empty;
    public static int Port { get; private set; }
    public static int Timeout { get; private set; }

    public static void Load()
    {
        IConfiguration config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        ServerIP = config["Client:ServerIP"] ?? string.Empty;
        Port = int.Parse(config["Client:Port"] ?? "5000");
        Timeout = int.Parse(config["Client:Timeout"] ?? "30000");
    }
}
}