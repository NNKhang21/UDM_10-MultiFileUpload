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
        try
        {
            Console.WriteLine("Client session started.");

            // Giả lập thời gian xử lý Client
            await Task.Delay(1000);

            Console.WriteLine("Client session ended.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Client error: {ex.Message}");
        }
        finally
        {
            _client.Close();
            Console.WriteLine("Client disconnected.");
        }
    }
}