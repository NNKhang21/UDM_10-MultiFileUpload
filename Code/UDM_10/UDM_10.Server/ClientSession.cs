using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UDM_10.Shared.Models;
using UDM_10.Shared.Protocol;

namespace UDM_10.Server
{
    
    public class ClientSession
    {
        private readonly TcpClient _client;
        private readonly NetworkStream _stream;
        private readonly FileStorageService _storage;

        public ClientSession(TcpClient client)
        {
            _client = client;
            _stream = client.GetStream();
            _storage = new FileStorageService();
        }

        public async Task RunAsync(CancellationToken token)
        {
            Console.WriteLine($"[Client Connected] {_client.Client.RemoteEndPoint}");

            try
            {
                while (_client.Connected && !token.IsCancellationRequested)
                {
                    // Đọc message từ client
                    MessageBase? message = await MessageFramer.ReadAsync(_stream, token);

                    if (message == null)
                    {
                        Console.WriteLine("[ClientSession] Client disconnected.");
                        break;
                    }

                    await HandleMessageAsync(message, token);
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine($"[IO Error] {ex.Message}");
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"[Socket Error] {ex.Message}");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("[ClientSession] Session cancelled.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Unexpected Error] {ex.Message}");
            }
            finally
            {
                Stop();
            }
        }

        private async Task HandleMessageAsync(MessageBase message, CancellationToken token)
        {
            switch (message)
            {
                case UploadStartMessage start:

                    Console.WriteLine($"Upload started: {start.FileName}");

                    await _storage.BeginUploadAsync(start);

                    await SendAckAsync(MessageType.UploadStartAck, token);

                    break;

                case UploadChunkMessage chunk:

                    Console.WriteLine($"Chunk #{chunk.ChunkIndex}");

                    await _storage.WriteChunkAsync(chunk);

                    await SendAckAsync(MessageType.UploadChunkAck, token);

                    break;

                case UploadDoneMessage done:

                    Console.WriteLine($"Upload completed: {done.FileName}");

                    bool success = await _storage.FinishUploadAsync(done);

                    await SendResultAsync(success, token);

                    break;

                default:

                    Console.WriteLine($"Unknown message: {message.GetType().Name}");

                    break;
            }
        }

        private async Task SendAckAsync(MessageType ackType, CancellationToken token)
        {
            var ack = new AckMessage
            {
                Type = ackType,
                Timestamp = DateTime.UtcNow
            };

            await MessageFramer.WriteAsync(_stream, ack, token);

            Console.WriteLine($"ACK -> {ackType}");
        }

        private async Task SendResultAsync(bool success, CancellationToken token)
        {
            var result = new UploadResultMessage
            {
                IsSuccess = success,
                Message = success
                    ? "Upload completed successfully."
                    : "Upload failed."
            };

            await MessageFramer.WriteAsync(_stream, result, token);

            Console.WriteLine($"RESULT -> {success}");
        }

        public void Stop()
        {
            try
            {
                _stream?.Close();
                _client?.Close();

                Console.WriteLine("[ClientSession] Connection closed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Close Error: {ex.Message}");
            }
        }
    }
}
