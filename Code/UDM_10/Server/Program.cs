using System.Net;
using System.Net.Sockets;

class Program
{
    static async Task Main(string[] args)
    {
        await RunAsync();
    }


    static async Task RunAsync()
    {
        TcpListener listener =
            new TcpListener(IPAddress.Any, 5000);

        listener.Start();

        Console.WriteLine("Server started...");
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