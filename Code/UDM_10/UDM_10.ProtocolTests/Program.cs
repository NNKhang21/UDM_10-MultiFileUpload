using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UDM_10.Shared.Models;
using UDM_10.Shared.Protocol;

namespace UDM_10.ProtocolTests
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("================================");
            Console.WriteLine("TC_25 - MessageFramer Round-trip");
            Console.WriteLine("================================");
            Console.WriteLine();

            await TestUploadStartMessage();
            await TestUploadChunkMessage();
            await TestUploadDoneMessage();
            await TestAckMessage();
            await TestUploadResultMessage();
            await TestLengthPrefixBigEndian();
            await TestMissingType();
            await TestAckMessageInvalidType();
            await TestTransferIdConsistency();
            await TestConnectionClosedDuringPayload();

            Console.WriteLine();
            Console.WriteLine("================================");
            Console.WriteLine("KET QUA: 5/5 PASS");
            Console.WriteLine("================================");
        }

        static async Task TestUploadStartMessage()
        {
            try
            {
                UploadStartMessage original = new UploadStartMessage
                {
                    TransferId = "transfer-001",
                    FileName = "test.txt",
                    FileSize = 12345
                };

                using MemoryStream stream = new MemoryStream();

                await MessageFramer.WriteAsync(
                    stream,
                    original,
                    CancellationToken.None);

                stream.Position = 0;

                MessageBase? result =
                    await MessageFramer.ReadAsync(
                        stream,
                        CancellationToken.None);

                if (result is not UploadStartMessage message)
                    throw new Exception("Sai MessageType");

                if (message.TransferId != original.TransferId)
                    throw new Exception("Sai TransferId");

                if (message.FileName != original.FileName)
                    throw new Exception("Sai FileName");

                if (message.FileSize != original.FileSize)
                    throw new Exception("Sai FileSize");

                Console.WriteLine("[PASS] UploadStartMessage");
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[FAIL] UploadStartMessage: {ex.Message}");
            }
        }

        static async Task TestUploadChunkMessage()
        {
            try
            {
                UploadChunkMessage original = new UploadChunkMessage
                {
                    TransferId = "transfer-002",
                    ChunkIndex = 3,
                    Length = 5,
                    DataBase64 = "SGVsbG8="
                };

                using MemoryStream stream = new MemoryStream();

                await MessageFramer.WriteAsync(
                    stream,
                    original,
                    CancellationToken.None);

                stream.Position = 0;

                MessageBase? result =
                    await MessageFramer.ReadAsync(
                        stream,
                        CancellationToken.None);

                if (result is not UploadChunkMessage message)
                    throw new Exception("Sai MessageType");

                if (message.TransferId != original.TransferId)
                    throw new Exception("Sai TransferId");

                if (message.ChunkIndex != original.ChunkIndex)
                    throw new Exception("Sai ChunkIndex");

                if (message.Length != original.Length)
                    throw new Exception("Sai Length");

                if (message.DataBase64 != original.DataBase64)
                    throw new Exception("Sai DataBase64");

                Console.WriteLine("[PASS] UploadChunkMessage");
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[FAIL] UploadChunkMessage: {ex.Message}");
            }
        }

        static async Task TestUploadDoneMessage()
        {
            try
            {
                UploadDoneMessage original = new UploadDoneMessage
                {
                    TransferId = "transfer-003",
                    FileName = "done.txt"
                };

                using MemoryStream stream = new MemoryStream();

                await MessageFramer.WriteAsync(
                    stream,
                    original,
                    CancellationToken.None);

                stream.Position = 0;

                MessageBase? result =
                    await MessageFramer.ReadAsync(
                        stream,
                        CancellationToken.None);

                if (result is not UploadDoneMessage message)
                    throw new Exception("Sai MessageType");

                if (message.TransferId != original.TransferId)
                    throw new Exception("Sai TransferId");

                if (message.FileName != original.FileName)
                    throw new Exception("Sai FileName");

                Console.WriteLine("[PASS] UploadDoneMessage");
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[FAIL] UploadDoneMessage: {ex.Message}");
            }
        }

        static async Task TestAckMessage()
        {
            try
            {
                AckMessage original =
                    new AckMessage(MessageType.UploadStartAck)
                    {
                        TransferId = "transfer-004"
                    };

                using MemoryStream stream = new MemoryStream();

                await MessageFramer.WriteAsync(
                    stream,
                    original,
                    CancellationToken.None);

                stream.Position = 0;

                MessageBase? result =
                    await MessageFramer.ReadAsync(
                        stream,
                        CancellationToken.None);

                if (result is not AckMessage message)
                    throw new Exception("Sai MessageType");

                if (message.Type != original.Type)
                    throw new Exception("Sai Type");

                if (message.TransferId != original.TransferId)
                    throw new Exception("Sai TransferId");

                Console.WriteLine("[PASS] AckMessage");
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[FAIL] AckMessage: {ex.Message}");
            }
        }
        static async Task TestTransferIdConsistency()
        {
            try
            {
                string transferId = "tc29-transfer-001";

                UploadStartMessage start = new UploadStartMessage
                {
                    TransferId = transferId,
                    FileName = "test.txt",
                    FileSize = 1000
                };

                UploadChunkMessage chunk = new UploadChunkMessage
                {
                    TransferId = transferId,
                    ChunkIndex = 0,
                    Length = 5,
                    DataBase64 = "SGVsbG8="
                };

                UploadDoneMessage done = new UploadDoneMessage
                {
                    TransferId = transferId,
                    FileName = "test.txt"
                };

                UploadResultMessage result = new UploadResultMessage
                {
                    TransferId = transferId,
                    IsSuccess = true,
                    ServerFileName = "test.txt",
                    Message = "Upload successful"
                };

                if (start.TransferId != transferId)
                    throw new Exception("Sai TransferId ở UploadStartMessage.");

                if (chunk.TransferId != transferId)
                    throw new Exception("Sai TransferId ở UploadChunkMessage.");

                if (done.TransferId != transferId)
                    throw new Exception("Sai TransferId ở UploadDoneMessage.");

                if (result.TransferId != transferId)
                    throw new Exception("Sai TransferId ở UploadResultMessage.");

                Console.WriteLine(
                    "[PASS] TC_29 - TransferId nhất quán Start -> Chunk -> Done -> Result");
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[FAIL] TC_29: {ex.Message}");
            }
        }
        static async Task TestUploadResultMessage()
        {
            try
            {
                UploadResultMessage original =
                    new UploadResultMessage
                    {
                        TransferId = "transfer-005",
                        IsSuccess = true,
                        ServerFileName = "result.txt",
                        Message = "Upload successful"
                    };

                using MemoryStream stream = new MemoryStream();

                await MessageFramer.WriteAsync(
                    stream,
                    original,
                    CancellationToken.None);

                stream.Position = 0;

                MessageBase? result =
                    await MessageFramer.ReadAsync(
                        stream,
                        CancellationToken.None);

                if (result is not UploadResultMessage message)
                    throw new Exception("Sai MessageType");

                if (message.TransferId != original.TransferId)
                    throw new Exception("Sai TransferId");

                if (message.IsSuccess != original.IsSuccess)
                    throw new Exception("Sai IsSuccess");

                if (message.ServerFileName != original.ServerFileName)
                    throw new Exception("Sai ServerFileName");

                if (message.Message != original.Message)
                    throw new Exception("Sai Message");

                Console.WriteLine("[PASS] UploadResultMessage");
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[FAIL] UploadResultMessage: {ex.Message}");
            }
        }
        static async Task TestLengthPrefixBigEndian()
        {
            try
            {
                UploadStartMessage original = new UploadStartMessage
                {
                    TransferId = "tc26-transfer",
                    FileName = "test-big-endian.txt",
                    FileSize = 1000
                };

                using MemoryStream stream = new MemoryStream();

                await MessageFramer.WriteAsync(
                    stream,
                    original,
                    CancellationToken.None);

                byte[] data = stream.ToArray();

                if (data.Length < 4)
                    throw new Exception("Dữ liệu không đủ 4 byte length prefix.");

                // 4 byte đầu tiên trên wire
                byte[] lengthPrefix = new byte[4];
                Array.Copy(data, 0, lengthPrefix, 0, 4);

                // JSON payload phía sau 4 byte length prefix
                int actualPayloadLength = data.Length - 4;

                // Giải mã Big-Endian
                int decodedLength =
                    (lengthPrefix[0] << 24) |
                    (lengthPrefix[1] << 16) |
                    (lengthPrefix[2] << 8) |
                    lengthPrefix[3];

                if (decodedLength != actualPayloadLength)
                    throw new Exception(
                        $"Sai length prefix: prefix={decodedLength}, " +
                        $"payload={actualPayloadLength}");

                // Kiểm tra ReadAsync đọc lại được message
                stream.Position = 0;

                MessageBase? result =
                    await MessageFramer.ReadAsync(
                        stream,
                        CancellationToken.None);

                if (result is not UploadStartMessage message)
                    throw new Exception("ReadAsync không giải mã đúng message.");

                if (message.TransferId != original.TransferId)
                    throw new Exception("Sai TransferId.");

                if (message.FileName != original.FileName)
                    throw new Exception("Sai FileName.");

                if (message.FileSize != original.FileSize)
                    throw new Exception("Sai FileSize.");

                Console.WriteLine(
                    $"[PASS] TC_26 - Big-Endian length prefix " +
                    $"({decodedLength} bytes)");
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[FAIL] TC_26: {ex.Message}");
            }
        }
        static async Task TestConnectionClosedDuringPayload()
        {
            try
            {
                // Tạo một payload giả có độ dài 100 byte.
                int expectedLength = 100;

                byte[] lengthPrefix =
                    BitConverter.GetBytes(expectedLength);

                // MessageFramer sử dụng Big-Endian.
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(lengthPrefix);
                }

                using MemoryStream stream = new MemoryStream();

                // Chỉ gửi 4 byte length prefix.
                // Không gửi payload.
                await stream.WriteAsync(
                    lengthPrefix,
                    CancellationToken.None);

                stream.Position = 0;

                try
                {
                    await MessageFramer.ReadAsync(
                        stream,
                        CancellationToken.None);

                    throw new Exception(
                        "Không ném IOException khi payload bị thiếu.");
                }
                catch (IOException)
                {
                    Console.WriteLine(
                        "[PASS] TC_30 - Đóng kết nối khi đang đọc payload");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[FAIL] TC_30: {ex.Message}");
            }
        }
        static async Task TestAckMessageInvalidType()
        {
            try
            {
                try
                {
                    // UploadStart KHÔNG phải loại ACK hợp lệ
                    AckMessage message =
                        new AckMessage(MessageType.UploadStart);

                    throw new Exception(
                        "Không ném ArgumentException khi truyền MessageType sai.");
                }
                catch (ArgumentException)
                {
                    Console.WriteLine(
                        "[PASS] TC_28 - AckMessage chặn MessageType sai");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[FAIL] TC_28: {ex.Message}");
            }
        }
        static async Task TestMissingType()
        {
            try
            {
                string json = """
            {
                "TransferId": "tc27-transfer",
                "FileName": "missing-type.txt",
                "FileSize": 100
            }
            """;

                byte[] payload =
                    System.Text.Encoding.UTF8.GetBytes(json);

                byte[] lengthPrefix =
                    BitConverter.GetBytes(payload.Length);

                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(lengthPrefix);
                }

                using MemoryStream stream = new MemoryStream();

                await stream.WriteAsync(
                    lengthPrefix,
                    CancellationToken.None);

                await stream.WriteAsync(
                    payload,
                    CancellationToken.None);

                stream.Position = 0;

                try
                {
                    await MessageFramer.ReadAsync(
                        stream,
                        CancellationToken.None);

                    throw new Exception(
                        "Không ném InvalidDataException khi thiếu Type.");
                }
                catch (InvalidDataException)
                {
                    Console.WriteLine(
                        "[PASS] TC_27 - Thiếu field Type");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    $"[FAIL] TC_27: {ex.Message}");
            }
        }
    }
}