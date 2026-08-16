using System;
using UDM_10.Shared.Protocol;

namespace UDM_10.Shared.Models
{
    public abstract class MessageBase
    {
        public MessageType Type { get; set; }
    }

    public class UploadStartMessage : MessageBase
    {
        public UploadStartMessage()
        {
            Type = MessageType.UploadStart;
        }

        // Mã định danh riêng cho từng lượt upload.
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

        // Dùng cùng TransferId của UploadStartMessage
        // để xác định chunk thuộc lượt upload nào.
        public string TransferId { get; set; } = string.Empty;

        public int ChunkIndex { get; set; }

        public int Length { get; set; }

        public string DataBase64 { get; set; } = string.Empty;
    }

    public class UploadDoneMessage : MessageBase
    {
        public UploadDoneMessage()
        {
            Type = MessageType.UploadDone;
        }

        // Giúp xác định lượt upload vừa hoàn thành.
        public string TransferId { get; set; } = string.Empty;

        public string FileName { get; set; } = string.Empty;
    }

    public class AckMessage : MessageBase
    {
        // Giữ constructor mặc định để tương thích với
        // quá trình deserialize JSON hiện tại.
        public AckMessage()
        {
        }

        // Khi chủ động tạo ACK, truyền rõ loại ACK.
        public AckMessage(MessageType type)
        {
            if (type != MessageType.UploadStartAck &&
                type != MessageType.UploadChunkAck)
            {
                throw new ArgumentException(
                    "AckMessage chỉ hỗ trợ UploadStartAck hoặc UploadChunkAck.",
                    nameof(type));
            }

            Type = type;
            Timestamp = DateTime.Now;
        }

        // Cho phép đối chiếu ACK với đúng lượt upload.
        public string TransferId { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; }
    }

    public class UploadResultMessage : MessageBase
    {
        public UploadResultMessage()
        {
            Type = MessageType.UploadResult;
        }

        // Giúp Client biết kết quả thuộc lượt upload nào.
        public string TransferId { get; set; } = string.Empty;

        public bool IsSuccess { get; set; }

        public string Message { get; set; } = string.Empty;
    }
}