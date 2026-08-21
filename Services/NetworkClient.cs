using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UDM_10.Client.Services
{
    // Định nghĩa kết quả upload và interface
    public record UploadOutcome(bool Success, string? ServerFileName, string? Message);

    public interface IFileUploader
    {
        Task<UploadOutcome> UploadFileAsync(string filePath, IProgress<double> progress, CancellationToken ct);
    }

    public class NetworkClient : IFileUploader
    {
        private TcpClient? _client;
        private NetworkStream? _stream;
        private const int ReadWriteTimeoutMs = 15000; // Thời gian chờ tối đa: 15 giây

        // Hàm kết nối đến Server
        public async Task<bool> ConnectAsync(string ipAddress, int port)
        {
            try
            {
                Disconnect(); // Đóng kết nối cũ nếu có
                _client = new TcpClient();

                // Cấu hình ngắt kết nối nếu mạng giật/treo quá 15s (Yêu cầu Tuần 4)
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

        // Hàm kiểm tra xem mạng còn sống không (Yêu cầu Tuần 4)
        private bool EnsureConnected()
        {
            return _client != null && _client.Connected && _stream != null;
        }

        // Hàm xử lý Upload File chính
        public async Task<UploadOutcome> UploadFileAsync(string filePath, IProgress<double> progress, CancellationToken ct)
        {
            // Kiểm tra kết nối trước khi gửi
            if (!EnsureConnected())
                return new UploadOutcome(false, null, "Chưa kết nối hoặc mất kết nối tới Server.");

            try
            {
                FileInfo fileInfo = new FileInfo(filePath);
                if (!fileInfo.Exists)
                    return new UploadOutcome(false, null, "File không tồn tại.");

                // Bước 1: Gửi Tên file và Dung lượng file sang Server
                byte[] fileNameBytes = Encoding.UTF8.GetBytes(fileInfo.Name);
                byte[] fileNameLengthBytes = BitConverter.GetBytes(fileNameBytes.Length);
                await _stream!.WriteAsync(fileNameLengthBytes, 0, fileNameLengthBytes.Length, ct);
                await _stream.WriteAsync(fileNameBytes, 0, fileNameBytes.Length, ct);

                byte[] fileSizeBytes = BitConverter.GetBytes(fileInfo.Length);
                await _stream.WriteAsync(fileSizeBytes, 0, fileSizeBytes.Length, ct);

                // Bước 2: Chia file thành các gói nhỏ (64KB) và gửi đi
                int bufferSize = 64 * 1024;
                byte[] buffer = new byte[bufferSize];
                long totalBytesSent = 0;
                long totalFileLength = fileInfo.Length;

                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    int bytesRead;
                    while ((bytesRead = await fs.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
                    {
                        // Kiểm tra nếu người dùng bấm nút Hủy
                        ct.ThrowIfCancellationRequested();

                        await _stream.WriteAsync(buffer, 0, bytesRead, ct);

                        // Cập nhật % tiến độ cho giao diện của Khang
                        totalBytesSent += bytesRead;
                        if (progress != null && totalFileLength > 0)
                        {
                            double percentage = (double)totalBytesSent / totalFileLength * 100;
                            progress.Report(percentage);
                        }
                    }
                }

                await _stream.FlushAsync(ct);

                // Trả về kết quả thành công
                return new UploadOutcome(true, fileInfo.Name, "Upload thành công.");
            }
            catch (OperationCanceledException)
            {
                // Xử lý khi bấm nút Hủy
                return new UploadOutcome(false, null, "Đã hủy tiến trình upload.");
            }
            catch (SocketException ex)
            {
                // Mất kết nối mạng giữa chừng (Yêu cầu Tuần 4)
                Disconnect();
                return new UploadOutcome(false, null, $"Lỗi mạng: {ex.Message}");
            }
            catch (IOException ex)
            {
                // Quá thời gian chờ / Treo mạng (Yêu cầu Tuần 4)
                Disconnect();
                return new UploadOutcome(false, null, $"Lỗi truyền dữ liệu (Timeout): {ex.Message}");
            }
            catch (Exception ex)
            {
                Disconnect();
                return new UploadOutcome(false, null, $"Lỗi không xác định: {ex.Message}");
            }
        }

        // Hàm ngắt và dọn dẹp kết nối sạch sẽ
        public void Disconnect()
        {
            try
            {
                _stream?.Close();
                _stream?.Dispose();
                _client?.Close();
                _client?.Dispose();
            }
            catch { }
            finally
            {
                _stream = null;
                _client = null;
            }
        }
    }
}