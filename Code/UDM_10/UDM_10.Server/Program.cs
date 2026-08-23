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
        private static FileStorageService? _storage;
        private static ServerConfig? _config;

        public static async Task Main(string[] args)
        {
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                _cts.Cancel();
            };

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

        private static async Task RunAsync(CancellationToken token)
        {
            _config = ServerConfig.Load("appsettings.json");

            Logger.Info(
                $"Config loaded: Host={_config.Host}, " +
                $"Port={_config.Port}, " +
                $"IdleTimeout={_config.IdleTimeoutSeconds}s");

            StartupCleanupService.RunStartupCleanup(_config);

            _storage = new FileStorageService(_config);

            IPAddress ip;

            if (!IPAddress.TryParse(_config.Host, out ip!))
            {
                Logger.Warn(
                    $"Invalid host '{_config.Host}'. " +
                    "Using IPAddress.Any.");

                ip = IPAddress.Any;
            }

            _listener = new TcpListener(ip, _config.Port);

            _listener.Server.SetSocketOption(
                SocketOptionLevel.Socket,
                SocketOptionName.ReuseAddress,
                true);

            _listener.Start();

            Logger.Info($"Server started at {ip}:{_config.Port}");

            Console.WriteLine("==============================");
            Console.WriteLine("       SERVER STARTED");
            Console.WriteLine("==============================");
            Console.WriteLine($"Host          : {_config.Host}");
            Console.WriteLine($"Port          : {_config.Port}");
            Console.WriteLine($"Idle Timeout  : {_config.IdleTimeoutSeconds}s");
            Console.WriteLine("==============================");
            Console.WriteLine();

            TimeSpan idleTimeout =
                TimeSpan.FromSeconds(_config.IdleTimeoutSeconds);

            while (!token.IsCancellationRequested)
            {
                TcpClient? client = null;

                try
                {
                    client = await _listener.AcceptTcpClientAsync(token);

                    Logger.Info(
                        $"Client connected: " +
                        $"{client.Client.RemoteEndPoint}");

                    ClientSession session =
                        new ClientSession(
                            client,
                            _storage,
                            idleTimeout);

                    lock (_lock)
                    {
                        _clients.Add(session);
                    }

                    _ = RunClientSessionAsync(session, token);
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

        private static void StopServer()
        {
            try
            {
                Logger.Info("Stopping server...");

                try
                {
                    _listener?.Stop();
                }
                catch (Exception ex)
                {
                    Logger.Warn(
                        $"Listener stop error: {ex.Message}");
                }

                List<ClientSession> clientsToStop;

                lock (_lock)
                {
                    clientsToStop =
                        new List<ClientSession>(_clients);

                    _clients.Clear();
                }

                foreach (ClientSession client in clientsToStop)
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
