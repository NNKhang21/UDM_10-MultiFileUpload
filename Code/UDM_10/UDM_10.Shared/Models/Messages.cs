using UDM_10.Shared.Protocol;

namespace UDM_10.Shared.Models
{
  
    public abstract class MessageBase
    {
        public MessageType Type { get; set; }
    }

    public class UploadStartMessage : MessageBase
    {
        public UploadStartMessage() { Type = MessageType.UploadStart; }
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
    }

    public class UploadChunkMessage : MessageBase
    {
        public UploadChunkMessage() { Type = MessageType.UploadChunk; }
        public int ChunkIndex { get; set; }
        public int Length { get; set; }
        // Dữ liệu chunk gửi kèm base64 trong JSON để đơn giản ở giai đoạn W2.
        // Có thể tối ưu sau bằng ReadRawAsync (đọc raw byte theo Length, không qua JSON).
        public string DataBase64 { get; set; } = string.Empty;
    }

    public class UploadDoneMessage : MessageBase
    {
        public UploadDoneMessage() { Type = MessageType.UploadDone; }
        public string FileName { get; set; } = string.Empty;
    }

    public class AckMessage : MessageBase
    {
        public System.DateTime Timestamp { get; set; }
    }

    public class UploadResultMessage : MessageBase
    {
        public UploadResultMessage() { Type = MessageType.UploadResult; }
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
