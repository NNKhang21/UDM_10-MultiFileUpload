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

        // TransferId của upload đang dở dang (nếu có), dùng để dọn dẹp file .part
        // khi Client mất kết nối đột ngột giữa chừng (rút mạng, tắt Client, timeout...).
        private string? _pendingTransferId;

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

        public async Task RunAsync(CancellationToken token)
        {
            string remoteEndPoint =
                _client.Client.RemoteEndPoint?.ToString()
                ?? "Unknown";

            Logger.Info($"[Client Connected] {remoteEndPoint}");

            try
            {
                while (!token.IsCancellationRequested)
                {
                    MessageBase? message = await ReadWithIdleTimeoutAsync(token);

                    if (message == null)
                    {
                        Logger.Info($"[Client Disconnected] {remoteEndPoint}");
                        break;
                    }

                    await HandleMessageAsync(message, token);
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
                Logger.Info($"[Session Cancelled] Client {remoteEndPoint}");
            }
            catch (Exception ex)
            {
                Logger.Warn(
                    $"[Unexpected Session Error] Client {remoteEndPoint}. " +
$"Details: {ex}");
            }
            finally
            {
                // Client rớt kết nối (rút mạng / tắt đột ngột / timeout) khi đang có
                // upload dở dang => dọn dẹp file .part, không để sót file khoá.
                AbortPendingUploadIfAny(remoteEndPoint);
                Stop();
            }
        }

        private void AbortPendingUploadIfAny(string remoteEndPoint)
        {
            string? transferId = Interlocked.Exchange(ref _pendingTransferId, null);
            if (string.IsNullOrEmpty(transferId))
                return;

            try
            {
                bool aborted = _storage.AbortUpload(transferId);
                if (aborted)
                {
                    Logger.Warn(
                        $"[Upload Aborted] Client {remoteEndPoint} disconnected " +
                        $"mid-upload. TransferId={transferId}. Cleaned up .part file.");
                }
            }
            catch (Exception ex)
            {
                Logger.Warn(
                    $"[Abort Cleanup Error] TransferId={transferId}. Details: {ex.Message}");
            }
        }

        private async Task<MessageBase?> ReadWithIdleTimeoutAsync(CancellationToken token)
        {
            using var timeoutCts =
                CancellationTokenSource.CreateLinkedTokenSource(token);

            timeoutCts.CancelAfter(_idleTimeout);

            try
            {
                return await MessageFramer.ReadAsync(_stream, timeoutCts.Token);
            }
            catch (OperationCanceledException)
                when (!token.IsCancellationRequested)
            {
                throw new TimeoutException(
                    $"Client exceeded idle timeout of " +
                    $"{_idleTimeout.TotalSeconds} seconds.");
            }
        }

        private async Task HandleMessageAsync(MessageBase message, CancellationToken token)
        {
            if (message == null)
            {
                Logger.Warn("[Protocol Warning] Received null message.");
                return;
            }

            switch (message)
            {
                case UploadStartMessage:
                case UploadChunkMessage:
                case UploadDoneMessage:

                    await HandleUploadAsync(message, token);
                    break;

                default:

                    Logger.Warn(
                        $"[Unknown Message] " +
                        $"Unsupported message type: " +
                        $"{message.GetType().FullName}. " +
                        $"Message will be ignored.");
                    break;
            }
        }

        private async Task HandleUploadAsync(MessageBase message, CancellationToken token)
        {
            try
            {
                switch (message)
                {
                    case UploadStartMessage start:
                        Logger.Info($"[Upload Start] File={start.FileName}");
                        await _storage.BeginUploadAsync(start, token);

                        // Đánh dấu đang có upload dở dang, để nếu Client rớt mạng
                        // giữa chừng, RunAsync biết đường gọi AbortUpload dọn .part.
                        Interlocked.Exchange(ref _pendingTransferId, start.TransferId);

                        await SendAckAsync(
                            MessageType.UploadStartAck,
                            start.TransferId,
                            token);

                        break;

                    case UploadChunkMessage chunk:

                        Logger.Info($"[Upload Chunk] Chunk={chunk.ChunkIndex}");

                        await _storage.WriteChunkAsync(chunk, token);

                        await SendAckAsync(
                            MessageType.UploadChunkAck,
                            chunk.TransferId,
                            token);

                        break;

                    case UploadDoneMessage done:

                        Logger.Info($"[Upload Done] File={done.FileName}");

                        // SUA: nhan dung tuple (bool, string) tu FinishUploadAsync
                        var (success, finalFileName) =
                            await _storage.FinishUploadAsync(done, token);

                        // Upload đã kết thúc (thành công hoặc thất bại) và
                        // FileStorageService đã tự dọn context/.part rồi,
                        // nên không cần AbortUpload lại khi session đóng nữa.
                        Interlocked.Exchange(ref _pendingTransferId, null);

                        // SUA: gui dung finalFileName ve Client, khong con hard-code null
                        await SendResultAsync(
                            success,
                            done.TransferId,
                            finalFileName,
                            token);

                        break;

                    default:

                        Logger.Warn(
                            $"[Upload Warning] Unsupported upload " +
                            $"message: {message.GetType().Name}");
                        break;
                }
            }
            catch (OperationCanceledException)
                when (token.IsCancellationRequested)
            {
                Logger.Info("[Upload Cancelled] Upload operation cancelled.");
                throw;
            }
            catch (IOException ex)
            {
                await HandleUploadErrorAsync($"I/O error during upload: {ex.Message}", token);
            }
            catch (SocketException ex)
            {
                await HandleUploadErrorAsync($"Socket error during upload: {ex.Message}", token);
            }
            catch (Exception ex)
            {
                await HandleUploadErrorAsync($"Upload processing error: {ex.Message}", token);
            }
        }
        private async Task HandleUploadErrorAsync(string errorMessage, CancellationToken token)
        {
            Logger.Warn($"[Upload Error] {errorMessage}");

            try
            {
                // SUA: Guid.Empty -> string.Empty, khop kieu du lieu TransferId
                await SendResultAsync(false, string.Empty, null, token);
            }
            catch (Exception ex)
            {
                Logger.Warn($"[Upload Error Response Failed] {ex.Message}");
            }
        }

        // SUA: transferId doi tu Guid sang string, khop dung UDM_10.Shared.Models.AckMessage.TransferId
        private async Task SendAckAsync(
            MessageType ackType,
            string transferId,
            CancellationToken token)
        {
            var ack = new AckMessage(ackType)
            {
                TransferId = transferId
            };

            await MessageFramer.WriteAsync(_stream, ack, token);

            Logger.Info($"[ACK -> Client] {ackType}");
        }

        // SUA: transferId doi tu Guid sang string
        private async Task SendResultAsync(
            bool success,
            string transferId,
            string? serverFileName,
            CancellationToken token)
        {
            var result = new UploadResultMessage
            {
                IsSuccess = success,
                TransferId = transferId,
                ServerFileName = serverFileName,
                Message = success
                    ? "Upload completed successfully."
                    : "Upload failed."
            };

            await MessageFramer.WriteAsync(_stream, result, token);
            Logger.Info($"[RESULT -> Client] Success={success}");
        }

        public void Stop()
        {
            if (Interlocked.Exchange(ref _stopped, 1) == 1)
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
                    Logger.Warn($"[Stream Close Error] {ex.Message}");
                }

                try
                {
                    _client.Close();
                }
                catch (Exception ex)
                {
                    Logger.Warn($"[Client Close Error] {ex.Message}");
                }

                Logger.Info("[ClientSession] Connection closed.");
            }
            catch (Exception ex)
            {
                Logger.Warn($"[Close Error] {ex.Message}");
            }
        }
    }
}