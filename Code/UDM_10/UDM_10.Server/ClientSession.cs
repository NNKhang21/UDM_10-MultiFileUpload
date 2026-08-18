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
        private readonly TimeSpan _idleTimeout;

        // FileStorageService được dùng chung cho toàn Server
        public ClientSession(
            TcpClient client,
            FileStorageService storage,
            TimeSpan idleTimeout)
        {
            _client = client;
            _stream = client.GetStream();
            _storage = storage;
            _idleTimeout = idleTimeout;
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
                        await ReadWithIdleTimeoutAsync(token);

                    if (message == null)
                    {
                        Logger.Info(
                            "[ClientSession] Client disconnected.");
                        break;
                    }

                    await HandleMessageAsync(message, token);
                }
            }
            catch (TimeoutException ex)
            {
                Logger.Warn(
                    $"[Idle Timeout] Client {_client.Client.RemoteEndPoint} " +
                    $"disconnected because it was idle for " +
                    $"{_idleTimeout.TotalSeconds} seconds. " +
                    $"Details: {ex.Message}");
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
                Stop();
            }
        }

        /// <summary>
        /// Đọc message từ Client với cơ chế IdleTimeout.
        /// Nếu Client không gửi message trong khoảng thời gian
        /// quy định thì session sẽ bị timeout.
        /// </summary>
        private async Task<MessageBase?> ReadWithIdleTimeoutAsync(
            CancellationToken token)
        {
            using var timeoutCts =
                CancellationTokenSource.CreateLinkedTokenSource(token);

            timeoutCts.CancelAfter(_idleTimeout);

            try
            {
                return await MessageFramer.ReadAsync(
                    _stream,
                    timeoutCts.Token);
            }
            catch (OperationCanceledException)
                when (!token.IsCancellationRequested)
            {
                throw new TimeoutException(
                    $"Client exceeded idle timeout of " +
                    $"{_idleTimeout.TotalSeconds} seconds.");
            }
        }

        /// <summary>
        /// Điều phối các message nhận được từ Client.
        /// </summary>
        private async Task HandleMessageAsync(
            MessageBase message,
            CancellationToken token)
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

        /// <summary>
        /// Xử lý toàn bộ nghiệp vụ Upload.
        /// </summary>
        private async Task HandleUploadAsync(
            MessageBase message,
            CancellationToken token)
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
                        $"Unsupported upload message: " +
                        $"{message.GetType().Name}");
                    break;
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

        /// <summary>
        /// Đóng ClientSession và giải phóng tài nguyên.
        /// </summary>
        public void Stop()
        {
            try
            {
                _stream.Close();
                _client.Close();

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
