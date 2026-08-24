using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using UDM_10.Shared.Models;

namespace UDM_10.Shared.Protocol
{
    public static class MessageFramer
    {
        // =========================================================
        // WRITE
        // =========================================================

        // Gửi message theo format:
        // [4-byte length prefix - Big Endian] + [JSON UTF-8]
        public static async Task WriteAsync(
            Stream stream,
            MessageBase message,
            CancellationToken token)
        {
            string json = Serialize(message);

            byte[] payload = Encoding.UTF8.GetBytes(json);

            byte[] lengthPrefix = BitConverter.GetBytes(payload.Length);

            // Chuyển độ dài sang Big-Endian.
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(lengthPrefix);
            }

            // Gửi 4 byte độ dài trước.
            await stream.WriteAsync(lengthPrefix, token);

            // Sau đó gửi JSON payload.
            await stream.WriteAsync(payload, token);

            await stream.FlushAsync(token);
        }

        // Đọc một message JSON hoàn chỉnh rồi deserialize thành MessageBase.
        public static async Task<MessageBase?> ReadAsync(
            Stream stream,
            CancellationToken token)
        {
            string? json = await ReadJsonAsync(stream, token);

            if (json == null)
            {
                return null;
            }

            return Deserialize(json);
        }

        // Overload ReadAsync có idle timeout.
        public static async Task<MessageBase?> ReadAsync(
            Stream stream,
            CancellationToken token,
            int idleTimeoutMs)
        {
            string? json =
                await ReadJsonAsync(
                    stream,
                    token,
                    idleTimeoutMs);

            if (json == null)
            {
                return null;
            }

            return Deserialize(json);
        }

        // =========================================================
        // READ JSON
        // =========================================================

        // Đọc JSON theo format:
        // [4-byte length prefix] + [JSON payload]
        //
        // Trả về null nếu phía bên kia đóng kết nối
        // trước khi bắt đầu một frame mới.
        public static async Task<string?> ReadJsonAsync(
            Stream stream,
            CancellationToken token)
        {
            // Bước 1:
            // Đọc chính xác 4 byte chứa độ dài JSON.
            byte[]? lengthBytes = await ReadExactAsync(
                stream,
                4,
                token);

            // Nếu chưa đọc byte nào mà kết nối đã đóng.
            if (lengthBytes == null)
            {
                return null;
            }

            // Bước 2:
            // Chuyển từ Big-Endian về định dạng phù hợp với máy.
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(lengthBytes);
            }

            int length = BitConverter.ToInt32(
                lengthBytes,
                0);

            // Độ dài âm là frame không hợp lệ.
            if (length < 0)
            {
                throw new InvalidDataException(
                    $"Độ dài JSON không hợp lệ: {length}");
            }

            // Cho phép payload rỗng.
            if (length == 0)
            {
                return string.Empty;
            }

            // Bước 3:
            // Đọc chính xác số byte JSON đã được khai báo.
            byte[]? payload = await ReadExactAsync(
                stream,
                length,
                token);

            // Nếu đang chờ payload mà kết nối bị đóng.
            if (payload == null)
            {
                throw new IOException(
                    "Kết nối bị đóng trước khi nhận được JSON payload.");
            }

            // Bước 4:
            // Chuyển byte UTF-8 trở lại thành chuỗi JSON.
            return Encoding.UTF8.GetString(payload);
        }
        // Overload ReadJsonAsync có idle timeout.
        public static async Task<string?> ReadJsonAsync(
            Stream stream,
            CancellationToken token,
            int idleTimeoutMs)
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(token);
            timeoutCts.CancelAfter(idleTimeoutMs);

            try
            {
                return await ReadJsonAsync(stream, timeoutCts.Token);
            }
            catch (OperationCanceledException) when (!token.IsCancellationRequested)
            {
                throw new IOException($"Timeout: không nhận được dữ liệu trong {idleTimeoutMs}ms.");
            }
        }
        // =========================================================
        // READ RAW
        // =========================================================

        // Đọc raw byte theo đúng độ dài chỉ định.
        // Dùng cho dữ liệu file/chunk không truyền qua JSON.
        public static async Task<byte[]?> ReadRawAsync(
            Stream stream,
            int length,
            CancellationToken token)
        {
            // Không cho phép độ dài âm.
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(length),
                    "Độ dài dữ liệu không được âm.");
            }

            // Nếu yêu cầu đọc 0 byte thì trả mảng rỗng.
            if (length == 0)
            {
                return Array.Empty<byte>();
            }

            return await ReadExactAsync(
                stream,
                length,
                token);
        }

        // TCP không đảm bảo một lần ReadAsync sẽ trả về đủ dữ liệu.
        //
        // Vì vậy phải đọc lặp cho đến khi nhận đủ count byte.
        private static async Task<byte[]?> ReadExactAsync(
            Stream stream,
            int count,
            CancellationToken token)
        {
            byte[] buffer = new byte[count];

            int offset = 0;

            while (offset < count)
            {
                int read = await stream.ReadAsync(
                    buffer.AsMemory(
                        offset,
                        count - offset),
                    token);

                if (read == 0)
                {
                    // Chưa nhận byte nào:
                    // phía bên kia đóng kết nối bình thường.
                    if (offset == 0)
                    {
                        return null;
                    }

                    // Đã nhận một phần nhưng kết nối bị đóng:
                    // frame đang truyền bị thiếu dữ liệu.
                    throw new IOException(
                        "Kết nối bị đóng khi đang đọc dữ liệu.");
                }

                offset += read;
            }

            return buffer;
        }

        // Chuyển các loại Message thành JSON.
        private static string Serialize(
            MessageBase message)
        {
            return message switch
            {
                UploadStartMessage m =>
                    JsonSerializer.Serialize(m),

                UploadChunkMessage m =>
                    JsonSerializer.Serialize(m),

                UploadDoneMessage m =>
                    JsonSerializer.Serialize(m),

                AckMessage m =>
                    JsonSerializer.Serialize(m),

                UploadResultMessage m =>
                    JsonSerializer.Serialize(m),

                _ => throw new NotSupportedException(
                    $"Không hỗ trợ serialize {message.GetType().Name}")
            };
        }

        // Chuyển JSON trở lại đúng loại Message.
        private static MessageBase Deserialize(
            string json)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(json))
                {
                    throw new InvalidDataException(
                        "Dữ liệu Protocol không hợp lệ: JSON rỗng.");
                }

            using JsonDocument doc =
                JsonDocument.Parse(json);

                if (doc.RootElement.ValueKind != JsonValueKind.Object)
                {
                    throw new InvalidDataException(
                        "Dữ liệu Protocol không hợp lệ: JSON phải là một object.");
                }

                if (!doc.RootElement.TryGetProperty(
                        "Type",
                        out JsonElement typeElement))
                {
                    throw new InvalidDataException(
                        "Dữ liệu Protocol không hợp lệ: thiếu trường 'Type'.");
                }

                if (typeElement.ValueKind != JsonValueKind.Number ||
                    !typeElement.TryGetInt32(out int typeValue))
                {
                    throw new InvalidDataException(
                        "Dữ liệu Protocol không hợp lệ: trường 'Type' phải là số nguyên.");
                }

            MessageType type =
                (MessageType)doc.RootElement
                    .GetProperty("Type")
                    .GetInt32();


                MessageBase? message = type switch
                {
                    MessageType.UploadStart =>
                        JsonSerializer.Deserialize<UploadStartMessage>(json),

                    MessageType.UploadChunk =>
                        JsonSerializer.Deserialize<UploadChunkMessage>(json),

                    MessageType.UploadDone =>
                        JsonSerializer.Deserialize<UploadDoneMessage>(json),

                    MessageType.UploadStartAck or MessageType.UploadChunkAck =>
                        JsonSerializer.Deserialize<AckMessage>(json),

                    MessageType.UploadResult =>
                        JsonSerializer.Deserialize<UploadResultMessage>(json),

                    _ => throw new NotSupportedException(
                        $"Không hỗ trợ deserialize MessageType = {type}")
                };
                if (message == null)
                {
                    throw new InvalidDataException(
                        "Dữ liệu Protocol không hợp lệ: không thể deserialize message.");
                }

                return message;
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException(
                    "Dữ liệu Protocol không hợp lệ: JSON sai cấu trúc.",
                    ex);
            }
            catch (KeyNotFoundException ex)
            {
                throw new InvalidDataException(
                    "Dữ liệu Protocol không hợp lệ: thiếu trường bắt buộc.",
                    ex);
            }
        }
    }
}