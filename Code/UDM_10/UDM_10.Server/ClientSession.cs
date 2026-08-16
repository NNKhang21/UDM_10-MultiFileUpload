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
        private readonly int _idleTimeoutMs;

        // Nhận FileStorageService dùng chung (singleton) từ Program.cs,
        // không tự new nữa (constructor thật cần ServerConfig).
        // idleTimeoutMs: truyền xuống ReceiveFileAsync để server không treo vô hạn
        // khi Client ngừng gửi dữ liệu giữa chừng (yêu cầu "IdleTimeout" của Nam
        // trong PhanCong_ThanhVien). GIẢ ĐỊNH: lấy từ config.IdleTimeoutMs ở Program.cs,
        // cần Cẩm Tiên xác nhận đúng tên field trong ServerConfig.cs.
        public ClientSession(TcpClient client, FileStorageService storage, int idleTimeoutMs = 0)
        {
            _client = client;
            _stream = client.GetStream();
            _storage = storage;
            _idleTimeoutMs = idleTimeoutMs;
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
                Logger.Error(
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
        /// Chỉ còn xử lý UploadStartMessage ở tầng message-dispatch:
        /// phần chunk được ReceiveFileAsync tự đọc thẳng từ _stream,
        /// không còn đi qua UploadChunkMessage/UploadDoneMessage nữa.
        /// </summary>
        private async Task HandleMessageAsync(
            MessageBase message,
            CancellationToken token)
        {
            try
            {
                switch (message)
                {
                    case UploadStartMessage start:

                        await HandleUploadAsync(start, token);
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
                Logger.Error(
                    $"[HandleMessage Error] {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Xử lý toàn bộ nghiệp vụ Upload cho 1 file:
        /// validate -> reserve target -> ack -> nhận toàn bộ chunk (1 lệnh gọi) -> gửi kết quả.
        /// </summary>
        private async Task HandleUploadAsync(
            UploadStartMessage start,
            CancellationToken token)
        {
            FileStream? partFile = null;
            string targetPath = string.Empty;

            try
            {
                Logger.Info(
                    $"Upload started: {start.FileName}");

                // TODO xác nhận với Tiến: UploadStartMessage có field FileSize không?
                // (dùng để validate + truyền expectedSize cho ReceiveFileAsync)
                _storage.ValidateFileName(start.FileName);
                _storage.ValidateFileSize(start.FileSize);

                (targetPath, partFile) =
                    await _storage.ReserveUploadTargetAsync(start.FileName, token);

                await SendAckAsync(
                    MessageType.UploadStartAck,
                    token);

                // ReceiveFileAsync tự đọc header + data từ _stream cho tới khi đủ expectedSize,
                // tự VerifyUpload + CompleteUpload bên trong, tự RollbackUpload + rethrow nếu lỗi.
                // Truyền _idleTimeoutMs để không treo vô hạn nếu Client ngừng gửi giữa chừng.
                string finalPath = await _storage.ReceiveFileAsync(
                    _stream,
                    targetPath,
                    partFile,
                    start.FileSize,
                    token,
                    _idleTimeoutMs);

                Logger.Info(
                    $"Upload completed: {finalPath}");

                await SendResultAsync(true, token);
            }
            catch (IOException ex)
            {
                Logger.Warn(
                    $"[Upload IO Error] {ex.Message}");

                await TrySendFailureResultAsync(token);
                throw;
            }
            catch (SocketException ex)
            {
                Logger.Warn(
                    $"[Upload Socket Error] {ex.Message}");

                await TrySendFailureResultAsync(token);
                throw;
            }
            catch (OperationCanceledException)
            {
                Logger.Warn(
                    "[Upload] Operation cancelled.");
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error(
                    $"[Upload Error] {ex.Message}");

                await TrySendFailureResultAsync(token);
                throw;
            }
        }

        private async Task TrySendFailureResultAsync(CancellationToken token)
        {
            try
            {
                await SendResultAsync(false, token);
            }
            catch (Exception ex)
            {
                Logger.Warn(
                    $"[SendFailureResult Error] {ex.Message}");
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
