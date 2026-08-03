using severcore;
using System.Net;
using System.Net.Sockets;
using UDM_10.Client.Shared.Config;

class Program
{
    static async Task Main(string[] args)
    {
        await RunAsync();
    }


    static async Task RunAsync()
    {
        // Load cấu hình từ appsettings.json
        ServerConfig.Load();


        TcpListener listener =
            new TcpListener(
                IPAddress.Any,
                ServerConfig.Port
            );


        listener.Start();


        Console.WriteLine("Server started...");
        Console.WriteLine($"IP: {ServerConfig.IP}");
        Console.WriteLine($"Port: {ServerConfig.Port}");
        Console.WriteLine("Waiting for client...");


        while (true)
        {
            TcpClient client =
                await listener.AcceptTcpClientAsync();


            Console.WriteLine("Client connected");


            ClientSession session =
                new ClientSession(client);


            _ = Task.Run(() => session.RunAsync());
        }
    }
}