using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UDM_10.Client.Server;
using UDM_10.Client.Shared.Config;

class Program
{
    private static TcpListener? _listener;
    private static readonly CancellationTokenSource _cts = new();

    static async Task Main(string[] args)
    {
        Console.CancelKeyPress += OnCancelKeyPress;

        try
        {
            await RunAsync(_cts.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Server stopped.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fatal error: {ex.Message}");
        }
        finally
        {
            StopServer();
        }
    }

    private static async Task RunAsync(CancellationToken token)
    {
        ServerConfig.Load();

        IPAddress ip;

        if (!IPAddress.TryParse(ServerConfig.IP, out ip))
        {
            ip = IPAddress.Any;
        }

        _listener = new TcpListener(ip, ServerConfig.Port);

        _listener.Start();

        Console.WriteLine("=================================");
        Console.WriteLine(" Server Started");
        Console.WriteLine("=================================");
        Console.WriteLine($"IP      : {ServerConfig.IP}");
        Console.WriteLine($"Port    : {ServerConfig.Port}");
        Console.WriteLine("Waiting for client...");
        Console.WriteLine();

        while (!token.IsCancellationRequested)
        {
            TcpClient? client = null;

            try
            {
                client = await _listener.AcceptTcpClientAsync();

                Console.WriteLine("---------------------------------");
                Console.WriteLine($"Client connected");
                Console.WriteLine($"Remote : {client.Client.RemoteEndPoint}");
                Console.WriteLine($"Time   : {DateTime.Now}");
                Console.WriteLine("---------------------------------");

                ClientSession session = new ClientSession(client);

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await session.RunAsync();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Session Error : {ex.Message}");
                    }
                });
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Socket Error : {ex.Message}");
            }
            catch (ObjectDisposedException)
            {
                break;
            }
        }
    }

    private static void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        e.Cancel = true;

        Console.WriteLine();
        Console.WriteLine("Stopping server...");

        _cts.Cancel();

        StopServer();
    }

    private static void StopServer()
    {
        try
        {
            _listener?.Stop();
        }
        catch
        {
        }
    }
}
