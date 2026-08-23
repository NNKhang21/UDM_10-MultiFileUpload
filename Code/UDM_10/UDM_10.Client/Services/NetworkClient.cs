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

    public class NetworkClient : IFileUploader
    {
        private TcpClient? _client;
        private NetworkStream? _stream;
        private const int ReadWriteTimeoutMs = 15000;

        public async Task<bool> ConnectAsync(string ipAddress, int port)
        {
            try
            {
                Disconnect();
                _client = new TcpClient();
                _client.SendTimeout = ReadWriteTimeoutMs;
                _client.ReceiveTimeout = ReadWriteTimeoutMs;
                await _client.ConnectAsync(ipAddress, port);
                _stream = _client.GetStream();
                return true;
            }
            catch (Exception)
            {
                Disconnect();
                return false;
            }
        }

        private bool EnsureConnected() => _client != null && _client.Connected && _stream != null;

        public async Task<UploadOutcome> UploadFileAsync(string filePath, IProgress<double> progress, CancellationToken ct)
        {
            if (!EnsureConnected())
                return new UploadOutcome(false, null, "Chưa kết nối hoặc mất kết nối tới Server.");

            var fileInfo = new FileInfo(filePath);
            if (!fileInfo.Exists)
                return new UploadOutcome(false, null, "File không tồn tại.");

            string transferId = Guid.NewGuid().ToString();   // MOI: 1 ma rieng cho lan upload nay

            try
            {
                var startMsg = new UploadStartMessage
                {
                    TransferId = transferId,
                    FileName = fileInfo.Name,
                    FileSize = fileInfo.Length
                };
                await MessageFramer.WriteAsync(_stream!, startMsg, ct);

                var startAck = await MessageFramer.ReadAsync(_stream!, ct);
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

                        await MessageFramer.WriteAsync(_stream!, chunkMsg, ct);

                        var chunkAck = await MessageFramer.ReadAsync(_stream!, ct);
                        if (chunkAck is not AckMessage)
                            return new UploadOutcome(false, fileInfo.Name, "Lỗi truyền tải chunk từ Server.");

                        totalBytesRead += bytesRead;
                        chunkIndex++;
                        progress?.Report((double)totalBytesRead / fileInfo.Length * 100);
                    }
                }

                var doneMsg = new UploadDoneMessage { TransferId = transferId, FileName = fileInfo.Name };
                await MessageFramer.WriteAsync(_stream!, doneMsg, ct);

                var resultMsg = await MessageFramer.ReadAsync(_stream!, ct) as UploadResultMessage;
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
                Disconnect();
                return new UploadOutcome(false, fileInfo.Name, $"Lỗi mạng: {ex.Message}");
            }
            catch (IOException ex)
            {
                Disconnect();
                return new UploadOutcome(false, fileInfo.Name, $"Lỗi truyền dữ liệu (Timeout): {ex.Message}");
            }
            catch (Exception ex)
            {
                Disconnect();
                return new UploadOutcome(false, fileInfo.Name, $"Lỗi không xác định: {ex.Message}");
            }
        }

        public void Disconnect()
        {
            try { _stream?.Close(); _stream?.Dispose(); _client?.Close(); _client?.Dispose(); }
            catch { }
            finally { _stream = null; _client = null; }
        }
    }
}
