using System;
using UDM_10.Shared.Protocol;

namespace UDM_10.Shared.Models
{
    // Base class dùng chung cho tất cả message trao đổi giữa Client và Server
    public abstract class MessageBase
    {
        public MessageType Type { get; protected set; }
    }

    // Client bắt đầu upload file
    public class UploadStartMessage : MessageBase
    {
        public UploadStartMessage()
        {
            Type = MessageType.UploadStart;
        }

        // ID riêng cho từng lượt upload - dùng xuyên suốt để phân biệt
        // các upload chạy song song
        public string TransferId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
    }

    public class UploadChunkMessage : MessageBase
    {
        public UploadChunkMessage()
        {
            Type = MessageType.UploadChunk;
        }

        // Dùng cùng TransferId của UploadStartMessage để xác định
        // chunk thuộc lượt upload nào
        public string TransferId { get; set; } = string.Empty;
        public int ChunkIndex { get; set; }
        public int Length { get; set; }
        public string DataBase64 { get; set; } = string.Empty;
    }

    // Client báo hoàn thành upload
    public class UploadDoneMessage : MessageBase
    {
        public UploadDoneMessage()
        {
            Type = MessageType.UploadDone;
        }

        public string TransferId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
    }

    public class AckMessage : MessageBase
    {
        // Giữ constructor mặc định để tương thích với quá trình
        // deserialize JSON (System.Text.Json cần constructor rỗng)
        public AckMessage() { }

        // Khi chủ động tạo ACK, bắt buộc truyền rõ loại ACK,
        // chặn ngay lúc build nếu ai đó lỡ truyền sai loại MessageType
        public AckMessage(MessageType type)
        {
            if (type != MessageType.UploadStartAck && type != MessageType.UploadChunkAck)
            {
                throw new ArgumentException(
                    "AckMessage chỉ hỗ trợ UploadStartAck hoặc UploadChunkAck.",
                    nameof(type));
            }

            Type = type;
            Timestamp = DateTime.Now;
        }
        internal void SetType(MessageType type)
        {
            Type = type;
        }

        // Cho phép đối chiếu ACK với đúng lượt upload
        public string TransferId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    // Server trả kết quả cuối cùng
    public class UploadResultMessage : MessageBase
    {
        public UploadResultMessage()
        {
            Type = MessageType.UploadResult;
        }

        // Giúp Client biết kết quả thuộc lượt upload nào
        public string TransferId { get; set; } = string.Empty;

        // Upload thành công hay thất bại
        public bool IsSuccess { get; set; }

        // Tên file thật trên Server sau khi xử lý trùng tên
        // Ví dụ: Client gửi "abc.txt", Server đổi thành "abc(1).txt"
        // Có thể null khi upload thất bại (không có tên cuối cùng để trả về)
        public string? ServerFileName { get; set; }

        // Nội dung thông báo
        public string Message { get; set; } = string.Empty;
    }
}