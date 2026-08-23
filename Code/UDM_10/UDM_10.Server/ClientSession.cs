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

        // Đảm bảo Stop() chỉ thực hiện cleanup một lần.
        private int _stopped;

        // FileStorageService được dùng chung cho toàn Server.
        public ClientSession(
            TcpClient client,
            FileStorageService storage,
            TimeSpan idleTimeout)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _idleTimeout = idleTimeout;

            _stream = _client.GetStream();
        }

        /// <summary>
        /// Chạy vòng đời của một ClientSession.
        ///
        /// Lifecycle:
        /// Connect
        ///     ↓
        /// Read Message
        ///     ↓
        /// Handle Message
        ///     ↓
        /// Read Message ...
        ///     ↓
        /// Disconnect / Timeout / Cancellation / Error
        ///     ↓
        /// Stop()
        /// </summary>
        public async Task RunAsync(CancellationToken token)
        {
            string remoteEndPoint =
                _client.Client.RemoteEndPoint?.ToString()
                ?? "Unknown";

            Logger.Info(
                $"[Client Connected] {remoteEndPoint}");

            try
            {
                while (!token.IsCancellationRequested)
                {
                    MessageBase? message =
                        await ReadWithIdleTimeoutAsync(token);

                    // null nghĩa là Client đã đóng kết nối.
                    if (message == null)
                    {
                        Logger.Info(
                            $"[Client Disconnected] {remoteEndPoint}");

                        break;
                    }

                    await HandleMessageAsync(
                        message,
                        token);
                }
            }
            catch (TimeoutException ex)
            {
                Logger.Warn(
                    $"[Idle Timeout] Client {remoteEndPoint} " +
                    $"was idle for {_idleTimeout.TotalSeconds} seconds. " +
                    $"Details: {ex.Message}");
            }
            catch (IOException ex)
            {
                Logger.Warn(
                    $"[IO Error] Client {remoteEndPoint} " +
                    $"disconnected. Details: {ex.Message}");
            }
            catch (SocketException ex)
            {
                Logger.Warn(
                    $"[Socket Error] Client {remoteEndPoint}. " +
                    $"Details: {ex.Message}");
            }
            catch (OperationCanceledException)
                when (token.IsCancellationRequested)
            {
                Logger.Info(
                    $"[Session Cancelled] Client {remoteEndPoint}");
            }
            catch (Exception ex)
            {
                // Exception của một Client chỉ ảnh hưởng session đó.
                Logger.Warn(
                    $"[Unexpected Session Error] Client {remoteEndPoint}. " +
                    $"Details: {ex}");
            }
            finally
            {
                Stop();
            }
        }

        /// <summary>
        /// Đọc message từ Client với cơ chế IdleTimeout.
        ///
        /// Nếu Client không gửi message trong khoảng thời gian
        /// _idleTimeout thì session bị timeout.
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
        /// Điều phối message nhận được từ Client.
        /// </summary>
        private async Task HandleMessageAsync(
            MessageBase message,
            CancellationToken token)
        {
            if (message == null)
            {
                Logger.Warn(
                    "[Protocol Warning] Received null message.");

                return;
            }

            switch (message)
            {
                case UploadStartMessage:
                case UploadChunkMessage:
                case UploadDoneMessage:

                    await HandleUploadAsync(
                        message,
                        token);

                    break;

                default:

                    // Message tồn tại nhưng ClientSession
                    // chưa hỗ trợ loại message này.
                    //
                    // Không được để message lạ làm Server crash.
                    Logger.Warn(
                        $"[Unknown Message] " +
                        $"Unsupported message type: " +
                        $"{message.GetType().FullName}. " +
                        $"Message will be ignored.");

                    break;
            }
        }

        /// <summary>
        /// Xử lý toàn bộ nghiệp vụ Upload.
        ///
        /// UploadStart
        ///     -> UploadStartAck
        ///
        /// UploadChunk
        ///     -> UploadChunkAck
        ///
        /// UploadDone
        ///     -> UploadResult
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
                            $"[Upload Start] File={start.FileName}");

                        await _storage.BeginUploadAsync(start);

                        await SendAckAsync(
                            MessageType.UploadStartAck,
                            token);

                        break;

                    case UploadChunkMessage chunk:

                        Logger.Info(
                            $"[Upload Chunk] " +
                            $"Chunk={chunk.ChunkIndex}");

                        await _storage.WriteChunkAsync(chunk);

                        await SendAckAsync(
                            MessageType.UploadChunkAck,
                            token);

                        break;

                    case UploadDoneMessage done:

                        Logger.Info(
                            $"[Upload Done] File={done.FileName}");

                        bool success =
                            await _storage.FinishUploadAsync(done);

                        await SendResultAsync(
                            success,
                            token);

                        break;

                    default:

                        // Trường hợp này gần như không xảy ra vì
                        // HandleMessageAsync đã lọc message.
                        Logger.Warn(
                            $"[Upload Warning] Unsupported upload " +
                            $"message: {message.GetType().Name}");

                        break;
                }
            }
            catch (OperationCanceledException)
                when (token.IsCancellationRequested)
            {
                Logger.Info(
                    "[Upload Cancelled] Upload operation cancelled.");

                throw;
            }
            catch (IOException ex)
            {
                await HandleUploadErrorAsync(
                    $"I/O error during upload: {ex.Message}",
                    token);
            }
            catch (SocketException ex)
            {
                await HandleUploadErrorAsync(
                    $"Socket error during upload: {ex.Message}",
                    token);
            }
            catch (Exception ex)
            {
                await HandleUploadErrorAsync(
                    $"Upload processing error: {ex.Message}",
                    token);
            }
        }

        /// <summary>
        /// Xử lý lỗi xảy ra trong quá trình Upload.
        ///
        /// Cố gắng thông báo thất bại cho Client trước khi
        /// session tiếp tục hoặc kết thúc.
        /// </summary>
        private async Task HandleUploadErrorAsync(
            string errorMessage,
            CancellationToken token)
        {
            Logger.Warn(
                $"[Upload Error] {errorMessage}");

            try
            {
                await SendResultAsync(
                    false,
                    token);
            }
            catch (Exception ex)
            {
                Logger.Warn(
                    $"[Upload Error Response Failed] " +
                    $"{ex.Message}");
            }
        }

        /// <summary>
        /// Gửi ACK về Client.
        /// </summary>
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
                $"[ACK -> Client] {ackType}");
        }

        /// <summary>
        /// Gửi kết quả Upload về Client.
        /// </summary>
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
                $"[RESULT -> Client] Success={success}");
        }

        /// <summary>
        /// Đóng ClientSession và giải phóng tài nguyên.
        ///
        /// Interlocked đảm bảo Stop() chỉ thực hiện một lần
        /// kể cả khi được gọi đồng thời từ nhiều nơi.
        /// </summary>
        public void Stop()
        {
            // Nếu đã Stop rồi thì không cleanup lần thứ hai.
            if (Interlocked.Exchange(
                    ref _stopped,
                    1) == 1)
            {
                return;
            }

            try
            {
                try
                {
                    _stream.Close();
                }
                catch (Exception ex)
                {
                    Logger.Warn(
                        $"[Stream Close Error] {ex.Message}");
                }

                try
                {
                    _client.Close();
                }
                catch (Exception ex)
                {
                    Logger.Warn(
                        $"[Client Close Error] {ex.Message}");
                }

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
