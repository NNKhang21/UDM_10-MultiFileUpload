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

        public ClientSession(TcpClient client)
        {
            _client = client;
            _stream = client.GetStream();
            _storage = new FileStorageService();
        }

        public async Task RunAsync(CancellationToken token)
        {
            Console.WriteLine(
                $"[Client Connected] {_client.Client.RemoteEndPoint}");

            try
            {
                // Mỗi ClientSession xử lý độc lập.
                // Không dùng _client.Connected để kiểm tra vòng lặp.
                while (!token.IsCancellationRequested)
                {
                    // Đọc message từ client
                    MessageBase? message =
                        await MessageFramer.ReadAsync(_stream, token);

                    // null = client đã disconnect
                    if (message == null)
                    {
                        Console.WriteLine(
                            "[ClientSession] Client disconnected.");
                        break;
                    }

                    // Xử lý message của riêng client này
                    await HandleMessageAsync(message, token);
                }
            }
            catch (IOException ex)
            {
                // Client có thể bị mất kết nối đột ngột
                Console.WriteLine(
                    $"[IO Error] Client disconnected: {ex.Message}");
            }
            catch (SocketException ex)
            {
                Console.WriteLine(
                    $"[Socket Error] {ex.Message}");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine(
                    "[ClientSession] Session cancelled.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[Unexpected Error] {ex.Message}");
            }
            finally
            {
                // Luôn giải phóng connection khi session kết thúc
                Stop();
            }
        }

        private async Task HandleMessageAsync(
            MessageBase message,
            CancellationToken token)
        {
            switch (message)
            {
                case UploadStartMessage start:

                    Console.WriteLine(
                        $"Upload started: {start.FileName}");

                    // Bắt đầu upload cho session hiện tại
                    await _storage.BeginUploadAsync(start);

                    // Gửi ACK cho UploadStart
                    await SendAckAsync(
                        MessageType.UploadStartAck,
                        token);

                    break;

                case UploadChunkMessage chunk:

                    Console.WriteLine(
                        $"Chunk #{chunk.ChunkIndex}");

                    // Ghi chunk vào file của session hiện tại
                    await _storage.WriteChunkAsync(chunk);

                    // Gửi ACK cho chunk
                    await SendAckAsync(
                        MessageType.UploadChunkAck,
                        token);

                    break;

                case UploadDoneMessage done:

                    Console.WriteLine(
                        $"Upload completed: {done.FileName}");

                    // Hoàn tất upload
                    bool success =
                        await _storage.FinishUploadAsync(done);

                    // Trả kết quả cho client
                    await SendResultAsync(
                        success,
                        token);

                    break;

                default:

                    Console.WriteLine(
                        $"Unknown message: {message.GetType().Name}");

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

            Console.WriteLine(
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

            Console.WriteLine(
                $"RESULT -> {success}");
        }

        public void Stop()
        {
            try
            {
                if (_stream != null)
                {
                    _stream.Close();
                }

                if (_client != null)
                {
                    _client.Close();
                }

                Console.WriteLine(
                    "[ClientSession] Connection closed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[Close Error] {ex.Message}");
            }
        }
    }
}
