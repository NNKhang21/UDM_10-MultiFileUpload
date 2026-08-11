using System;
using System.Collections.Generic;
using System.IO;               // Thêm thư viện này
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;        // Thêm thư viện này
using System.Threading.Tasks;

namespace UDM_10.Client.Services
{
    public class NetworkClient
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

        public async Task<bool> UploadFileAsync(string filePath, int chunkSizeKb = 64, CancellationToken cancellationToken = default)
        {
            if (_stream == null || !_client.Connected)
                return false;

            try
            {
                FileInfo fileInfo = new FileInfo(filePath);
                if (!fileInfo.Exists) return false;

                // 1. Gửi thông tin Metadata (Tên file & Dung lượng file)
                byte[] fileNameBytes = Encoding.UTF8.GetBytes(fileInfo.Name);
                byte[] fileNameLengthBytes = BitConverter.GetBytes(fileNameBytes.Length);
                await _stream.WriteAsync(fileNameLengthBytes, 0, fileNameLengthBytes.Length, cancellationToken);
                await _stream.WriteAsync(fileNameBytes, 0, fileNameBytes.Length, cancellationToken);

                byte[] fileSizeBytes = BitConverter.GetBytes(fileInfo.Length);
                await _stream.WriteAsync(fileSizeBytes, 0, fileSizeBytes.Length, cancellationToken);

                // 2. Đọc file và gửi dữ liệu theo từng ChunkSizeKb (Mặc định 64KB)
                int bufferSize = chunkSizeKb * 1024;
                byte[] buffer = new byte[bufferSize];

                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    int bytesRead;
                    while ((bytesRead = await fs.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                    {
                        await _stream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                    }
                }

                await _stream.FlushAsync(cancellationToken);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Ngắt kết nối và dọn dẹp tài nguyên
        /// </summary>
        public void Disconnect()
        {
            _stream?.Close();
            _client?.Close();
        }
    }
}