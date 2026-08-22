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
        public System.Guid TransferId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
    }

    public class UploadChunkMessage : MessageBase
    {
        public UploadChunkMessage() { Type = MessageType.UploadChunk; }
        public System.Guid TransferId { get; set; }
        public int ChunkIndex { get; set; }
        public int Length { get; set; }
        public string DataBase64 { get; set; } = string.Empty;
    }

    public class UploadDoneMessage : MessageBase
    {
        public UploadDoneMessage() { Type = MessageType.UploadDone; }
        public System.Guid TransferId { get; set; }
        public string FileName { get; set; } = string.Empty;
    }

    public class AckMessage : MessageBase
    {
        public System.Guid TransferId { get; set; }
        public System.DateTime Timestamp { get; set; }
    }

    public class UploadResultMessage : MessageBase
    {
        public UploadResultMessage() { Type = MessageType.UploadResult; }
        public System.Guid TransferId { get; set; }
        public bool IsSuccess { get; set; }
        public string? ServerFileName { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
