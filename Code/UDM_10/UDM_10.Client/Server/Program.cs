using System;
using System.Collections.Generic;
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

    // Quản lý nhiều client
    private static readonly List<ClientSession> _clients = new();

    private static readonly object _lock = new();


    static async Task Main(string[] args)
    {
        Console.CancelKeyPress += OnCancelKeyPress;


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



    private static async Task RunAsync(
        CancellationToken token)
    {

        ServerConfig.Load();


        if(ServerConfig.Port <= 0 ||
           ServerConfig.Port > 65535)
        {
            throw new Exception(
                "Invalid server port.");
        }



        IPAddress ip;

        if(!IPAddress.TryParse(
            ServerConfig.IP,
            out ip))
        {
            ip = IPAddress.Any;
        }



        _listener =
            new TcpListener(
                ip,
                ServerConfig.Port);



        _listener.Server.SetSocketOption(
            SocketOptionLevel.Socket,
            SocketOptionName.ReuseAddress,
            true);



        _listener.Start();



        Logger.Info(
            $"Server started {ip}:{ServerConfig.Port}");

        Console.WriteLine("==============================");
        Console.WriteLine(" SERVER STARTED");
        Console.WriteLine("==============================");
        Console.WriteLine($"IP   : {ip}");
        Console.WriteLine($"Port : {ServerConfig.Port}");
        Console.WriteLine();



        while(!token.IsCancellationRequested)
        {

            TcpClient? client = null;


            try
            {

                client =
                    await _listener
                    .AcceptTcpClientAsync(token);



                Logger.Info(
                    $"Client connected: {client.Client.RemoteEndPoint}");



                ClientSession session =
                    new ClientSession(client);



                lock(_lock)
                {
                    _clients.Add(session);
                }



                _ = HandleClientAsync(
                    session,
                    client,
                    token);

            }


            catch(OperationCanceledException)
            {
                break;
            }


            catch(SocketException ex)
            {

                if(!token.IsCancellationRequested)
                {
                    Logger.Error(
                        $"Socket error: {ex.Message}");
                }

            }


            catch(Exception ex)
            {
                Logger.Error(
                    $"Accept error: {ex.Message}");
            }

        }

    }





    private static async Task HandleClientAsync(
        ClientSession session,
        TcpClient client,
        CancellationToken serverToken)
    {

        try
        {

            // Timeout 30 giây
            using CancellationTokenSource timeout =
                CancellationTokenSource
                .CreateLinkedTokenSource(serverToken);



            timeout.CancelAfter(
                TimeSpan.FromSeconds(30));



            await session.RunAsync(
                timeout.Token);

        }



        catch(OperationCanceledException)
        {
            Logger.Info(
                "Client timeout.");
        }


        catch(SocketException ex)
        {
            Logger.Info(
                $"Client disconnected: {ex.Message}");
        }


        catch(IOException ex)
        {
            Logger.Info(
                $"Connection lost: {ex.Message}");
        }


        catch(Exception ex)
        {
            Logger.Error(
                $"Session error: {ex.Message}");
        }



        finally
        {

            lock(_lock)
            {
                _clients.Remove(session);
            }



            try
            {
                client.Close();
                client.Dispose();
            }
            catch
            {

            }



            Logger.Info(
                "Client resource released.");

        }

    }






    private static void OnCancelKeyPress(
        object? sender,
        ConsoleCancelEventArgs e)
    {

        e.Cancel = true;


        Logger.Info(
            "Stopping server...");


        _cts.Cancel();


        StopServer();

    }






    private static void StopServer()
    {

        try
        {


            // Đóng tất cả client
            lock(_lock)
            {

                foreach(var client in _clients)
                {
                    try
                    {
                        client.Stop();
                    }
                    catch
                    {

                    }
                }


                _clients.Clear();

            }




            _listener?.Stop();



            Logger.Info(
                $"Server shutdown {DateTime.Now}");


            Console.WriteLine();
            Console.WriteLine("==============================");
            Console.WriteLine(" SERVER SHUTDOWN");
            Console.WriteLine("==============================");

        }


        catch(Exception ex)
        {

            Logger.Error(
                $"Shutdown error: {ex.Message}");

        }

    }

}
