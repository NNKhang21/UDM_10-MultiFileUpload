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

            byte[] payload =
                Encoding.UTF8.GetBytes(json);

            byte[] lengthPrefix =
                BitConverter.GetBytes(payload.Length);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(lengthPrefix);
            }

            await stream.WriteAsync(
                lengthPrefix,
                token);

            await stream.WriteAsync(
                payload,
                token);

            await stream.FlushAsync(token);
        }

        // Overload có timeout khi gửi message.
        public static async Task WriteAsync(
            Stream stream,
            MessageBase message,
            CancellationToken token,
            int idleTimeoutMs)
        {
            ValidateIdleTimeout(idleTimeoutMs);

            using CancellationTokenSource timeoutCts =
                CancellationTokenSource.CreateLinkedTokenSource(token);

            timeoutCts.CancelAfter(idleTimeoutMs);

            try
            {
                await WriteAsync(
                    stream,
                    message,
                    timeoutCts.Token);
            }
            catch (OperationCanceledException)
                when (!token.IsCancellationRequested &&
                      timeoutCts.IsCancellationRequested)
            {
                throw new TimeoutException(
                    $"Quá thời gian gửi message ({idleTimeoutMs} ms).");
            }
        }

        // =========================================================
        // READ MESSAGE
        // =========================================================

        // Đọc một message JSON hoàn chỉnh
        // rồi deserialize thành MessageBase.
        public static async Task<MessageBase?> ReadAsync(
            Stream stream,
            CancellationToken token)
        {
            string? json =
                await ReadJsonAsync(
                    stream,
                    token);

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
        public static async Task<string?> ReadJsonAsync(
            Stream stream,
            CancellationToken token)
        {
            byte[]? lengthBytes =
                await ReadExactAsync(
                    stream,
                    4,
                    token);

            if (lengthBytes == null)
            {
                return null;
            }

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(lengthBytes);
            }

            int length =
                BitConverter.ToInt32(
                    lengthBytes,
                    0);

            if (length < 0)
            {
                throw new InvalidDataException(
                    $"Độ dài JSON không hợp lệ: {length}");
            }

            if (length == 0)
            {
                return string.Empty;
            }

            byte[]? payload =
                await ReadExactAsync(
                    stream,
                    length,
                    token);

            if (payload == null)
            {
                throw new IOException(
                    "Kết nối bị đóng trước khi nhận được JSON payload.");
            }

            return Encoding.UTF8.GetString(payload);
        }

        // ReadJsonAsync có idle timeout.
        public static async Task<string?> ReadJsonAsync(
            Stream stream,
            CancellationToken token,
            int idleTimeoutMs)
        {
            ValidateIdleTimeout(idleTimeoutMs);

            byte[]? lengthBytes =
                await ReadWithIdleTimeoutAsync(
                    stream,
                    4,
                    token,
                    idleTimeoutMs);

            if (lengthBytes == null)
            {
                return null;
            }

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(lengthBytes);
            }

            int length =
                BitConverter.ToInt32(
                    lengthBytes,
                    0);

            if (length < 0)
            {
                throw new InvalidDataException(
                    $"Độ dài JSON không hợp lệ: {length}");
            }

            if (length == 0)
            {
                return string.Empty;
            }

            byte[]? payload =
                await ReadWithIdleTimeoutAsync(
                    stream,
                    length,
                    token,
                    idleTimeoutMs);

            if (payload == null)
            {
                throw new IOException(
                    "Kết nối bị đóng trước khi nhận được JSON payload.");
            }

            return Encoding.UTF8.GetString(payload);
        }

        // =========================================================
        // READ RAW
        // =========================================================

        // Đọc raw byte theo đúng độ dài chỉ định.
        public static async Task<byte[]?> ReadRawAsync(
            Stream stream,
            int length,
            CancellationToken token)
        {
            ValidateLength(length);

            if (length == 0)
            {
                return Array.Empty<byte>();
            }

            return await ReadExactAsync(
                stream,
                length,
                token);
        }

        // ReadRawAsync có idle timeout.
        public static async Task<byte[]?> ReadRawAsync(
            Stream stream,
            int length,
            CancellationToken token,
            int idleTimeoutMs)
        {
            ValidateLength(length);
            ValidateIdleTimeout(idleTimeoutMs);

            if (length == 0)
            {
                return Array.Empty<byte>();
            }

            return await ReadWithIdleTimeoutAsync(
                stream,
                length,
                token,
                idleTimeoutMs);
        }

        // =========================================================
        // READ EXACT - KHÔNG TIMEOUT
        // =========================================================

        // TCP không đảm bảo một lần ReadAsync trả đủ dữ liệu.
        // Vì vậy phải đọc lặp cho đến khi nhận đủ count byte.
        private static async Task<byte[]?> ReadExactAsync(
            Stream stream,
            int count,
            CancellationToken token)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(count),
                    "Số byte cần đọc không được âm.");
            }

            byte[] buffer =
                new byte[count];

            int offset = 0;

            while (offset < count)
            {
                int read =
                    await stream.ReadAsync(
                        buffer.AsMemory(
                            offset,
                            count - offset),
                        token);

                if (read == 0)
                {
                    if (offset == 0)
                    {
                        return null;
                    }

                    throw new IOException(
                        "Kết nối bị đóng khi đang đọc dữ liệu.");
                }

                offset += read;
            }

            return buffer;
        }

        // =========================================================
        // READ WITH IDLE TIMEOUT
        // =========================================================

        // Đọc đủ số byte yêu cầu.
        // Mỗi lần nhận được dữ liệu sẽ bắt đầu lại idle timeout.
        private static async Task<byte[]?> ReadWithIdleTimeoutAsync(
            Stream stream,
            int count,
            CancellationToken token,
            int idleTimeoutMs)
        {
            ValidateIdleTimeout(idleTimeoutMs);

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(count),
                    "Số byte cần đọc không được âm.");
            }

            byte[] buffer =
                new byte[count];

            int offset = 0;

            while (offset < count)
            {
                using CancellationTokenSource timeoutCts =
                    CancellationTokenSource.CreateLinkedTokenSource(token);

                timeoutCts.CancelAfter(idleTimeoutMs);

                int read;

                try
                {
                    read =
                        await stream.ReadAsync(
                            buffer.AsMemory(
                                offset,
                                count - offset),
                            timeoutCts.Token);
                }
                catch (OperationCanceledException)
                    when (!token.IsCancellationRequested &&
                          timeoutCts.IsCancellationRequested)
                {
                    throw new TimeoutException(
                        $"Không nhận được dữ liệu trong {idleTimeoutMs} ms.");
                }

                if (read == 0)
                {
                    if (offset == 0)
                    {
                        return null;
                    }

                    throw new IOException(
                        "Kết nối bị đóng khi đang đọc dữ liệu.");
                }

                offset += read;
            }

            return buffer;
        }

        // =========================================================
        // VALIDATION
        // =========================================================

        private static void ValidateLength(
            int length)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(length),
                    "Độ dài dữ liệu không được âm.");
            }
        }

        private static void ValidateIdleTimeout(
            int idleTimeoutMs)
        {
            if (idleTimeoutMs <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(idleTimeoutMs),
                    "Idle timeout phải lớn hơn 0 ms.");
            }
        }

        // =========================================================
        // SERIALIZE
        // =========================================================

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

        // =========================================================
        // DESERIALIZE
        // =========================================================

        // Deserialize JSON thành message.
        // Nếu dữ liệu sai cấu trúc thì trả lỗi rõ ràng.
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
                    (MessageType)typeValue;

                MessageBase? message =
                    type switch
                    {
                        MessageType.UploadStart =>
                            JsonSerializer.Deserialize<UploadStartMessage>(
                                json),

                        MessageType.UploadChunk =>
                            JsonSerializer.Deserialize<UploadChunkMessage>(
                                json),

                        MessageType.UploadDone =>
                            JsonSerializer.Deserialize<UploadDoneMessage>(
                                json),

                        MessageType.UploadStartAck or
                        MessageType.UploadChunkAck =>
                            JsonSerializer.Deserialize<AckMessage>(
                                json),

                        MessageType.UploadResult =>
                            JsonSerializer.Deserialize<UploadResultMessage>(
                                json),

                        _ => throw new InvalidDataException(
                            $"Dữ liệu Protocol không hợp lệ: MessageType không được hỗ trợ ({typeValue}).")
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
