using System;
using System.Threading.Tasks;
using UDM_10.Client.Server;

class Program
{
    static async Task Main(string[] args)
    {
        TcpServer server = new TcpServer();

        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            server.Stop();
        };


        await server.StartAsync();
    }
}
