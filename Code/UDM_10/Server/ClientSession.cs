using System.Net.Sockets;

class ClientSession
{
    private readonly TcpClient _client;

    public ClientSession(TcpClient client)
    {
        _client = client;
    }


    public async Task RunAsync()
    {
        Console.WriteLine("Client session started.");

        await Task.Delay(1000);

        Console.WriteLine("Client session ended.");

        _client.Close();
    }
}