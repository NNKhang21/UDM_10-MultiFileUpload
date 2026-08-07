using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using UDM_10.Shared.Models;

namespace UDM_10.Shared.Protocol
{
    // NOTE (fix): file này trước đó trống 0 byte. Bản tối thiểu dưới đây implement
    // đúng format đã mô tả trong kế hoạch: 4-byte length prefix (Big-Endian) + JSON.
    // Nga (Protocol) nên rà lại/mở rộng theo đúng thiết kế cuối cùng của nhóm.
    public static class MessageFramer
    {
        public static async Task WriteAsync(Stream stream, MessageBase message, CancellationToken token)
        {
            string json = Serialize(message);
            byte[] payload = Encoding.UTF8.GetBytes(json);
            byte[] lengthPrefix = BitConverter.GetBytes(payload.Length);
            if (BitConverter.IsLittleEndian) Array.Reverse(lengthPrefix);

            await stream.WriteAsync(lengthPrefix, token);
            await stream.WriteAsync(payload, token);
            await stream.FlushAsync(token);
        }

        public static async Task<MessageBase?> ReadAsync(Stream stream, CancellationToken token)
        {
            string? json = await ReadJsonAsync(stream, token);
            if (json == null) return null;
            return Deserialize(json);
        }

        // Đọc phần header JSON (được prefix bằng 4-byte length). Trả về null nếu client đã đóng kết nối.
        public static async Task<string?> ReadJsonAsync(Stream stream, CancellationToken token)
        {
            byte[]? lengthBytes = await ReadExactAsync(stream, 4, token);
            if (lengthBytes == null) return null;

            if (BitConverter.IsLittleEndian) Array.Reverse(lengthBytes);
            int length = BitConverter.ToInt32(lengthBytes, 0);
            if (length <= 0) return string.Empty;

            byte[]? payload = await ReadExactAsync(stream, length, token);
            if (payload == null) return null;

            return Encoding.UTF8.GetString(payload);
        }

        // Đọc raw byte theo độ dài chỉ định (dùng cho chunk data không qua JSON, tối ưu sau).
        public static async Task<byte[]?> ReadRawAsync(Stream stream, int length, CancellationToken token)
        {
            return await ReadExactAsync(stream, length, token);
        }

        private static async Task<byte[]?> ReadExactAsync(Stream stream, int count, CancellationToken token)
        {
            byte[] buffer = new byte[count];
            int offset = 0;
            while (offset < count)
            {
                int read = await stream.ReadAsync(buffer.AsMemory(offset, count - offset), token);
                if (read == 0)
                {
                    // Client đóng kết nối giữa chừng
                    return offset == 0 ? null : throw new IOException("Kết nối bị đóng khi đang đọc dữ liệu.");
                }
                offset += read;
            }
            return buffer;
        }

        private static string Serialize(MessageBase message)
        {
            return message switch
            {
                UploadStartMessage m => JsonSerializer.Serialize(m),
                UploadChunkMessage m => JsonSerializer.Serialize(m),
                UploadDoneMessage m => JsonSerializer.Serialize(m),
                AckMessage m => JsonSerializer.Serialize(m),
                UploadResultMessage m => JsonSerializer.Serialize(m),
                _ => throw new NotSupportedException($"Không hỗ trợ serialize {message.GetType().Name}")
            };
        }

        private static MessageBase Deserialize(string json)
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            MessageType type = (MessageType)doc.RootElement.GetProperty("Type").GetInt32();

            return type switch
            {
                MessageType.UploadStart => JsonSerializer.Deserialize<UploadStartMessage>(json)!,
                MessageType.UploadChunk => JsonSerializer.Deserialize<UploadChunkMessage>(json)!,
                MessageType.UploadDone => JsonSerializer.Deserialize<UploadDoneMessage>(json)!,
                MessageType.UploadStartAck or MessageType.UploadChunkAck => JsonSerializer.Deserialize<AckMessage>(json)!,
                MessageType.UploadResult => JsonSerializer.Deserialize<UploadResultMessage>(json)!,
                _ => throw new NotSupportedException($"Không hỗ trợ deserialize MessageType {type}")
            };
        }
    }
}
