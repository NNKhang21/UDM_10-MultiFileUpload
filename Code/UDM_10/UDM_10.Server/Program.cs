using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UDM_10.Shared.Config;

namespace UDM_10.Server
{
    public class Program
    {
        private static TcpListener? _listener;

        private static readonly CancellationTokenSource _cts = new();

        private static readonly List<ClientSession> _clients = new();

        private static readonly object _lock = new();

        // Một FileStorageService dùng chung cho toàn bộ Server
        private static FileStorageService? _storage;

        private static ServerConfig? _config;

        public static async Task Main(string[] args)
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
                Logger.Error($"Fatal server error: {ex}");
            }
            finally
            {
                StopServer();
            }
        }

        private static async Task RunAsync(
            CancellationToken token)
        {
            // ==========================================
            // 1. LOAD CONFIG
            // ==========================================

            _config = ServerConfig.Load("appsettings.json");

            Logger.Info(
                $"Config loaded: Host={_config.Host}, " +
                $"Port={_config.Port}, " +
                $"IdleTimeout={_config.IdleTimeoutSeconds}s");

            // ==========================================
            // 2. STARTUP CLEANUP
            // ==========================================

            StartupCleanupService.RunStartupCleanup(_config);

            // ==========================================
            // 3. CREATE SHARED STORAGE SERVICE
            // ==========================================

            _storage = new FileStorageService(_config);

            // ==========================================
            // 4. CREATE TCP LISTENER
            // ==========================================

            IPAddress ip;

            if (!IPAddress.TryParse(
                    _config.Host,
                    out ip!))
            {
                Logger.Warn(
                    $"Invalid host '{_config.Host}'. " +
                    "Using IPAddress.Any.");

                ip = IPAddress.Any;
            }

            _listener = new TcpListener(
                ip,
                _config.Port);

            _listener.Server.SetSocketOption(
                SocketOptionLevel.Socket,
                SocketOptionName.ReuseAddress,
                true);

            _listener.Start();

            Logger.Info(
                $"Server started at " +
                $"{ip}:{_config.Port}");

            Console.WriteLine("==============================");
            Console.WriteLine("       SERVER STARTED");
            Console.WriteLine("==============================");
            Console.WriteLine(
                $"Host          : {_config.Host}");
            Console.WriteLine(
                $"Port          : {_config.Port}");
            Console.WriteLine(
                $"Idle Timeout  : {_config.IdleTimeoutSeconds}s");
            Console.WriteLine("==============================");
            Console.WriteLine();

            // ==========================================
            // 5. CREATE IDLE TIMEOUT
            // ==========================================

            TimeSpan idleTimeout =
                TimeSpan.FromSeconds(
                    _config.IdleTimeoutSeconds);

            // ==========================================
            // 6. ACCEPT CLIENT LOOP
            // ==========================================

            while (!token.IsCancellationRequested)
            {
                TcpClient? client = null;

                try
                {
                    client =
                        await _listener.AcceptTcpClientAsync(
                            token);

                    Logger.Info(
                        $"Client connected: " +
                        $"{client.Client.RemoteEndPoint}");

                    // ==================================
                    // 7. CREATE CLIENT SESSION
                    // ==================================

                    ClientSession session =
                        new ClientSession(
                            client,
                            _storage,
                            idleTimeout);

                    lock (_lock)
                    {
                        _clients.Add(session);
                    }

                    // ==================================
                    // 8. RUN CLIENT INDEPENDENTLY
                    // ==================================

                    _ = RunClientSessionAsync(
                        session,
                        token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (SocketException ex)
                {
                    Logger.Error(
                        $"Accept socket error: {ex.Message}");

                    client?.Close();
                }
                catch (Exception ex)
                {
                    Logger.Error(
                        $"Accept client error: {ex.Message}");

                    client?.Close();
                }
            }
        }

        /// <summary>
        /// Chạy một ClientSession độc lập.
        /// Exception của một Client không làm Server chết.
        /// </summary>
        private static async Task RunClientSessionAsync(
            ClientSession session,
            CancellationToken token)
        {
            try
            {
                await session.RunAsync(token);
            }
            catch (Exception ex)
            {
                Logger.Warn(
                    $"Client session error: {ex.Message}");
            }
            finally
            {
                lock (_lock)
                {
                    _clients.Remove(session);
                }

                Logger.Info(
                    "Client session removed from Server.");
            }
        }

        /// <summary>
        /// Đóng Server và tất cả ClientSession.
        /// </summary>
        private static void StopServer()
        {
            try
            {
                Logger.Info(
                    "Stopping server...");

                // ==========================================
                // 1. STOP LISTENER
                // ==========================================

                try
                {
                    _listener?.Stop();
                }
                catch (Exception ex)
                {
                    Logger.Warn(
                        $"Listener stop error: {ex.Message}");
                }

                // ==========================================
                // 2. CLOSE ALL CLIENTS
                // ==========================================

                lock (_lock)
                {
                    foreach (ClientSession client in _clients)
                    {
                        try
                        {
                            client.Stop();
                        }
                        catch (Exception ex)
                        {
                            Logger.Warn(
                                $"Client cleanup error: " +
                                $"{ex.Message}");
                        }
                    }

                    _clients.Clear();
                }

                Logger.Info(
                    $"Server shutdown completed " +
                    $"at {DateTime.Now}");
            }
            catch (Exception ex)
            {
                Logger.Error(
                    $"Shutdown error: {ex.Message}");
            }
        }
    }
}
