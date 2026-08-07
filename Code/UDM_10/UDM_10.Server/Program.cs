using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UDM_10.Shared.Config;

namespace UDM_10.Server
{
    // NOTE (fix): trước đây file này KHÔNG có namespace, và "using UDM_10.Client.Server"
    // trỏ sai chỗ -> không thấy ClientSession (namespace UDM_10.Server). Đã thêm
    // namespace UDM_10.Server để khớp với ClientSession.cs, FileStorageService.cs, Logger.cs.
    // Đồng thời: trước đây file này và UDM_10.Client/Program.cs cùng có Main() trong
    // CÙNG 1 project -> lỗi "multiple entry points". Đã tách Server thành project
    // UDM_10.Server riêng (console app) để có thể chạy độc lập với Client (đúng như
    // kế hoạch "Server + Client chạy 2 tiến trình riêng").
    class Program
    {
        private static TcpListener? _listener;
        private static readonly CancellationTokenSource _cts = new();
        private static readonly List<ClientSession> _clients = new();
        private static readonly object _lock = new();

        static async Task Main(string[] args)
        {
            Logger.Init();

            try
            {
                await RunAsync(_cts.Token);
            }
            catch (OperationCanceledException)
            {
                Logger.Info("Server stopped.");
            }
            catch (Exception ex)
            {
                Logger.Error($"Fatal error: {ex.Message}");
            }
            finally
            {
                StopServer();
            }
        }

        private static async Task RunAsync(CancellationToken token)
        {
            ServerConfig.Load();

            if (!IPAddress.TryParse(ServerConfig.IP, out IPAddress? ip))
            {
                ip = IPAddress.Any;
            }

            _listener = new TcpListener(ip, ServerConfig.Port);
            _listener.Server.SetSocketOption(
                SocketOptionLevel.Socket,
                SocketOptionName.ReuseAddress,
                true);

            _listener.Start();

            Logger.Info($"Server started {ip}:{ServerConfig.Port}");

            Console.WriteLine("==============================");
            Console.WriteLine(" SERVER STARTED");
            Console.WriteLine("==============================");
            Console.WriteLine();

            while (!token.IsCancellationRequested)
            {
                try
                {
                    TcpClient client = await _listener.AcceptTcpClientAsync(token);

                    Logger.Info($"Client connected: {client.Client.RemoteEndPoint}");

                    ClientSession session = new ClientSession(client);

                    lock (_lock)
                    {
                        _clients.Add(session);
                    }

                    // Chạy mỗi ClientSession trên một Task riêng
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await session.RunAsync(token);
                        }
                        finally
                        {
                            // Khi session kết thúc thì xóa khỏi danh sách
                            lock (_lock)
                            {
                                _clients.Remove(session);
                            }
                        }
                    }, token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (SocketException ex)
                {
                    Logger.Error($"Socket error: {ex.Message}");
                }
            }
        }

        private static void StopServer()
        {
            try
            {
                lock (_lock)
                {
                    foreach (var client in _clients)
                    {
                        try
                        {
                            client.Stop();
                        }
                        catch
                        {
                            // Ignore cleanup errors
                        }
                    }

                    _clients.Clear();
                }

                _listener?.Stop();

                Logger.Info($"Server shutdown {DateTime.Now}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Shutdown error: {ex.Message}");
            }
        }
    }
}
