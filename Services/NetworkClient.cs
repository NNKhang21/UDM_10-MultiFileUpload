using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UDM_10.Client.Services
{
    // Record & Interface theo đúng yêu cầu của Khang
    public record UploadOutcome(bool Success, string? ServerFileName, string? Message);

    public interface IFileUploader
    {
        Task<UploadOutcome> UploadFileAsync(string filePath, IProgress<double> progress, CancellationToken ct);
    }

    public class NetworkClient : IFileUploader
    {
        private TcpClient _client;
        private NetworkStream _stream;

        public async Task<bool> ConnectAsync(string ipAddress, int port)
        {
            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(ipAddress, port);
                _stream = _client.GetStream();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Hàm UploadFileAsync đã cập nhật theo interface IFileUploader
        /// </summary>
        public async Task<UploadOutcome> UploadFileAsync(string filePath, IProgress<double> progress, CancellationToken ct)
        {
            if (_stream == null || !_client.Connected)
                return new UploadOutcome(false, null, "Chưa kết nối tới Server.");

            try
            {
                FileInfo fileInfo = new FileInfo(filePath);
                if (!fileInfo.Exists)
                    return new UploadOutcome(false, null, "File không tồn tại.");

                // 1. Gửi thông tin Metadata (Tên file & Dung lượng file)
                byte[] fileNameBytes = Encoding.UTF8.GetBytes(fileInfo.Name);
                byte[] fileNameLengthBytes = BitConverter.GetBytes(fileNameBytes.Length);
                await _stream.WriteAsync(fileNameLengthBytes, 0, fileNameLengthBytes.Length, ct);
                await _stream.WriteAsync(fileNameBytes, 0, fileNameBytes.Length, ct);

                byte[] fileSizeBytes = BitConverter.GetBytes(fileInfo.Length);
                await _stream.WriteAsync(fileSizeBytes, 0, fileSizeBytes.Length, ct);

                // 2. Đọc file và gửi dữ liệu theo từng Chunk (Mặc định 64KB)
                int bufferSize = 64 * 1024;
                byte[] buffer = new byte[bufferSize];
                long totalBytesSent = 0;
                long totalFileLength = fileInfo.Length;

                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    int bytesRead;
                    while ((bytesRead = await fs.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
                    {
                        // Kiểm tra nếu người dùng bấm Hủy
                        ct.ThrowIfCancellationRequested();

                        await _stream.WriteAsync(buffer, 0, bytesRead, ct);

                        // Báo tiến độ % về cho UI (Khang)
                        totalBytesSent += bytesRead;
                        if (progress != null && totalFileLength > 0)
                        {
                            double percentage = (double)totalBytesSent / totalFileLength * 100;
                            progress.Report(percentage);
                        }
                    }
                }

                await _stream.FlushAsync(ct);

                // TODO: Đọc phản hồi (Ack/Result) từ Server gửi về 
                // Xem Server có đổi tên file hay không (Ví dụ: "Tailieu (1).pdf")
                // Hiện tại giả định Server giữ nguyên tên file:
                string serverSavedFileName = fileInfo.Name;

                return new UploadOutcome(true, serverSavedFileName, "Upload thành công.");
            }
            catch (OperationCanceledException)
            {
                return new UploadOutcome(false, null, "Đã hủy tiến trình upload.");
            }
            catch (Exception ex)
            {
                return new UploadOutcome(false, null, $"Lỗi upload: {ex.Message}");
            }
        }

        public void Disconnect()
        {
            _stream?.Close();
            _client?.Close();
        }
    }
}