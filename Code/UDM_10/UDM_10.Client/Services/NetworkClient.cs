using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UDM_10.Shared.Models;
using UDM_10.Shared.Protocol;

namespace UDM_10.Client.Services
{
    public record UploadOutcome(bool Success, string? ServerFileName, string? Message);

    public interface IFileUploader
    {
        Task<UploadOutcome> UploadFileAsync(string filePath, IProgress<double> progress, CancellationToken ct);
    }

    // FIX RACE CONDITION:
    // NetworkClient trước đây dùng CHUNG 1 TcpClient / NetworkStream cho tất cả file upload.
    // Khi UploadManager chạy 5 file song song, 5 thread cùng gọi WriteAsync/ReadAsync lên cùng 1 stream
    // → bytes message bị ghi đè xen kẽ → protocol bị vỡ → Server gửi lỗi → Client đọc rác → exception →
    // tự gọi Disconnect() đóng socket → Server log "forcibly closed by remote host".
    //
    // GIẢI PHÁP: Mỗi file upload (mỗi lần gọi UploadFileAsync) sẽ tự mở 1 TcpClient RIÊNG, tự connect,
    // upload xong thì tự đóng. Như vậy không có sự chia sẻ stream => không có race condition.
    // ConnectAsync ban đầu chỉ dùng để TEST connectivity (ping Server coi chạy không) thôi,
    // không giữ lại để upload.
    public class NetworkClient : IFileUploader
    {
        private string _ipAddress = "127.0.0.1";
        private int _port = 9000;
        private const int ReadWriteTimeoutMs = 30000;
        private const int ConnectTimeoutMs = 5000;

        public string? LastError { get; private set; }

        // Test connection only (nút Connect bấm lần đầu). Sau đó không giữ kết nối này để upload.
        public async Task<bool> ConnectAsync(string ipAddress, int port)
        {
            LastError = null;
            _ipAddress = ipAddress;
            _port = port;
            try
            {
                using var testClient = new TcpClient();
                testClient.SendTimeout = ReadWriteTimeoutMs;
                testClient.ReceiveTimeout = ReadWriteTimeoutMs;

                using var cts = new CancellationTokenSource(ConnectTimeoutMs);
                await testClient.ConnectAsync(ipAddress, port, cts.Token);
                return true;
            }
            catch (OperationCanceledException)
            {
                LastError = $"Timeout: không thể kết nối tới {ipAddress}:{port} trong {ConnectTimeoutMs}ms. Đảm bảo Server đang chạy.";
                return false;
            }
            catch (SocketException ex)
            {
                LastError = $"Lỗi Socket ({ex.SocketErrorCode}): {ex.Message}. Có thể Server chưa chạy hoặc sai IP/Port.";
                return false;
            }
            catch (Exception ex)
            {
                LastError = $"Kết nối thất bại: {ex.Message}";
                return false;
            }
        }

        // Luôn tạo TcpClient mới cho mỗi file upload.
        private async Task<TcpClient> OpenNewConnection(CancellationToken outerCt)
        {
            var client = new TcpClient();
            client.SendTimeout = ReadWriteTimeoutMs;
            client.ReceiveTimeout = ReadWriteTimeoutMs;

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(outerCt);
            cts.CancelAfter(ConnectTimeoutMs);

            await client.ConnectAsync(_ipAddress, _port, cts.Token);
            return client;
        }

        public async Task<UploadOutcome> UploadFileAsync(string filePath, IProgress<double> progress, CancellationToken ct)
        {
            var fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists)
                return new UploadOutcome(false, null, "File không tồn tại.");

            string transferId = Guid.NewGuid().ToString();

            TcpClient? perFileClient = null;
            NetworkStream? stream = null;
            try
            {
                perFileClient = await OpenNewConnection(ct);
                stream = perFileClient.GetStream();

                var startMsg = new UploadStartMessage
                {
                    TransferId = transferId,
                    FileName = fileInfo.Name,
                    FileSize = fileInfo.Length
                };
                await MessageFramer.WriteAsync(stream, startMsg, ct);

                var startAck = await MessageFramer.ReadAsync(stream, ct);
                if (startAck is not AckMessage)
                    return new UploadOutcome(false, fileInfo.Name, "Server từ chối bắt đầu upload.");

                const int chunkSize = 64 * 1024;
                byte[] buffer = new byte[chunkSize];
                long totalBytesRead = 0;
                int chunkIndex = 0;

                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    int bytesRead;
                    while ((bytesRead = await fs.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
                    {
                        ct.ThrowIfCancellationRequested();
                        byte[] actualBytes = new byte[bytesRead];
                        Array.Copy(buffer, actualBytes, bytesRead);

                        var chunkMsg = new UploadChunkMessage
                        {
                            TransferId = transferId,
                            ChunkIndex = chunkIndex,
                            Length = bytesRead,
                            DataBase64 = Convert.ToBase64String(actualBytes)
                        };

                        await MessageFramer.WriteAsync(stream, chunkMsg, ct);

                        var chunkAck = await MessageFramer.ReadAsync(stream, ct);
                        if (chunkAck is not AckMessage)
                            return new UploadOutcome(false, fileInfo.Name, "Lỗi truyền tải chunk từ Server.");

                        totalBytesRead += bytesRead;
                        chunkIndex++;
                        progress?.Report((double)totalBytesRead / fileInfo.Length * 100);
                    }
                }

                var doneMsg = new UploadDoneMessage { TransferId = transferId, FileName = fileInfo.Name };
                await MessageFramer.WriteAsync(stream, doneMsg, ct);

                var resultMsg = await MessageFramer.ReadAsync(stream, ct) as UploadResultMessage;
                if (resultMsg != null)
                {
                    return new UploadOutcome(resultMsg.IsSuccess, resultMsg.ServerFileName ?? fileInfo.Name, resultMsg.Message);
                }

                return new UploadOutcome(false, fileInfo.Name, "Không nhận được phản hồi kết quả từ Server.");
            }
            catch (OperationCanceledException)
            {
                return new UploadOutcome(false, fileInfo.Name, "Đã hủy tiến trình upload.");
            }
            catch (SocketException ex)
            {
                return new UploadOutcome(false, fileInfo.Name, $"Lỗi mạng: {ex.Message}");
            }
            catch (IOException ex)
            {
                return new UploadOutcome(false, fileInfo.Name, $"Lỗi truyền dữ liệu (Timeout): {ex.Message}");
            }
            catch (Exception ex)
            {
                return new UploadOutcome(false, fileInfo.Name, $"Lỗi không xác định: {ex.Message}");
            }
            finally
            {
                try { stream?.Close(); stream?.Dispose(); } catch { }
                try { perFileClient?.Close(); perFileClient?.Dispose(); } catch { }
            }
        }

        // Giả lập Disconnect() cho tương thích API cũ (MainForm.btnDisconnect_Click gọi).
        // Sau refactor "per-file per-connection", NetworkClient không giữ kết nối chung nữa,
        // nên method này không còn đóng TCP thật sự (vì không có), mà chỉ đánh dấu "đã ngắt".
        // Cancel các upload chạy cần phải qua UploadManager.CancelAll() (quyết định từ UI).
        public void Disconnect()
        {
            // Reset IP/Port về mặc định (tùy chọn, không bắt buộc) nhưng giữ lại để có thể reconnect
            // _ipAddress = "127.0.0.1"; _port = 9000;
        }
    }
}
