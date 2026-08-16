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

        // FileStorageService giờ được truyền vào (dùng chung 1 instance
        // cho toàn Server), thay vì mỗi session tự "new" một cái riêng.
        public ClientSession(TcpClient client, FileStorageService storage)
        {
            _client = client;
            _stream = client.GetStream();
            _storage = storage;
        }

        public async Task RunAsync(CancellationToken token)
        {
            Logger.Info(
                $"[Client Connected] {_client.Client.RemoteEndPoint}");

            try
            {
                while (!token.IsCancellationRequested)
                {
                    MessageBase? message =
                        await MessageFramer.ReadAsync(_stream, token);

                    // Client đã ngắt kết nối
                    if (message == null)
                    {
                        Logger.Info(
                            "[ClientSession] Client disconnected.");
                        break;
                    }

                    await HandleMessageAsync(message, token);
                }
            }
            catch (IOException ex)
            {
                Logger.Warn(
                    $"[IO Error] Client disconnected: {ex.Message}");
            }
            catch (SocketException ex)
            {
                Logger.Warn(
                    $"[Socket Error] {ex.Message}");
            }
            catch (OperationCanceledException)
            {
                Logger.Info(
                    "[ClientSession] Session cancelled.");
            }
            catch (Exception ex)
            {
                Logger.Warn(
                    $"[Unexpected Error] {ex.Message}");
            }
            finally
            {
                // Luôn giải phóng tài nguyên
                Stop();
            }
        }

        /// <summary>
        /// Điều phối các message nhận được từ Client.
        /// </summary>
        private async Task HandleMessageAsync(
            MessageBase message,
            CancellationToken token)
        {
            try
            {
                switch (message)
                {
                    case UploadStartMessage:
                    case UploadChunkMessage:
                    case UploadDoneMessage:

                        await HandleUploadAsync(message, token);
                        break;

                    default:

                        Logger.Warn(
                            $"Unknown message: {message.GetType().Name}");
                        break;
                }
            }
            catch (IOException ex)
            {
                Logger.Warn(
                    $"[HandleMessage IO Error] {ex.Message}");
                throw;
            }
            catch (SocketException ex)
            {
                Logger.Warn(
                    $"[HandleMessage Socket Error] {ex.Message}");
                throw;
            }
            catch (Exception ex)
            {
                Logger.Warn(
                    $"[HandleMessage Error] {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Xử lý toàn bộ nghiệp vụ Upload.
        /// </summary>
        private async Task HandleUploadAsync(
            MessageBase message,
            CancellationToken token)
        {
            try
            {
                switch (message)
                {
                    case UploadStartMessage start:

                        Logger.Info(
                            $"Upload started: {start.FileName}");

                        await _storage.BeginUploadAsync(start);

                        await SendAckAsync(
                            MessageType.UploadStartAck,
                            token);

                        break;

                    case UploadChunkMessage chunk:

                        Logger.Info(
                            $"Chunk #{chunk.ChunkIndex}");

                        await _storage.WriteChunkAsync(chunk);

                        await SendAckAsync(
                            MessageType.UploadChunkAck,
                            token);

                        break;

                    case UploadDoneMessage done:

                        Logger.Info(
                            $"Upload completed: {done.FileName}");

                        bool success =
                            await _storage.FinishUploadAsync(done);

                        await SendResultAsync(
                            success,
                            token);

                        break;

                    default:

                        Logger.Warn(
                            $"Unsupported upload message: {message.GetType().Name}");

                        break;
                }
            }
            catch (IOException ex)
            {
                Logger.Warn(
                    $"[Upload IO Error] {ex.Message}");
                throw;
            }
            catch (SocketException ex)
            {
                Logger.Warn(
                    $"[Upload Socket Error] {ex.Message}");
                throw;
            }
            catch (OperationCanceledException)
            {
                Logger.Info(
                    "[Upload] Operation cancelled.");
                throw;
            }
            catch (Exception ex)
            {
                Logger.Warn(
                    $"[Upload Error] {ex.Message}");
                throw;
            }
        }

        private async Task SendAckAsync(
            MessageType ackType,
            CancellationToken token)
        {
            var ack = new AckMessage
            {
                Type = ackType,
                Timestamp = DateTime.UtcNow
            };

            await MessageFramer.WriteAsync(
                _stream,
                ack,
                token);

            Logger.Info(
                $"ACK -> {ackType}");
        }

        private async Task SendResultAsync(
            bool success,
            CancellationToken token)
        {
            var result = new UploadResultMessage
            {
                IsSuccess = success,
                Message = success
                    ? "Upload completed successfully."
                    : "Upload failed."
            };

            await MessageFramer.WriteAsync(
                _stream,
                result,
                token);

            Logger.Info(
                $"RESULT -> {success}");
        }

        public void Stop()
        {
            try
            {
                _stream?.Close();
                _client?.Close();

                Logger.Info(
                    "[ClientSession] Connection closed.");
            }
            catch (Exception ex)
            {
                Logger.Warn(
                    $"[Close Error] {ex.Message}");
            }
        }
    }
}
