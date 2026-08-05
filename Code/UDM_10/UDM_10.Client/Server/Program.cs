using System;
using System.IO;
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

        // Kiểm tra Port hợp lệ
        if (ServerConfig.Port <= 0 || ServerConfig.Port > 65535)
        {
            throw new Exception("Invalid server port.");
        }

        IPAddress ip;

        if (!IPAddress.TryParse(ServerConfig.IP, out ip))
        {
            ip = IPAddress.Any;
        }

        _listener = new TcpListener(ip, ServerConfig.Port);

        // Cho phép mở lại server ngay sau khi tắt
        _listener.Server.SetSocketOption(
            SocketOptionLevel.Socket,
            SocketOptionName.ReuseAddress,
            true);

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

                // Timeout
                client.ReceiveTimeout = 30000;
                client.SendTimeout = 30000;

                Console.WriteLine("---------------------------------");
                Console.WriteLine("Client connected");
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
                    catch (SocketException)
                    {
                        Console.WriteLine($"Client disconnected: {client.Client.RemoteEndPoint}");
                    }
                    catch (IOException)
                    {
                        Console.WriteLine($"Connection lost: {client.Client.RemoteEndPoint}");
                    }
                    catch (ObjectDisposedException)
                    {
                        Console.WriteLine($"Connection closed: {client.Client.RemoteEndPoint}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Session Error: {ex.Message}");
                    }
                    finally
                    {
                        try
                        {
                            client.Close();
                            client.Dispose();
                        }
                        catch
                        {
                        }

                        Console.WriteLine($"Client released: {DateTime.Now}");
                    }
                });
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Socket Error: {ex.Message}");
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

            Console.WriteLine("=================================");
            Console.WriteLine(" Server Shutdown");
            Console.WriteLine($"Time : {DateTime.Now}");
            Console.WriteLine("=================================");
        }
        catch
        {
        }
    }
}
