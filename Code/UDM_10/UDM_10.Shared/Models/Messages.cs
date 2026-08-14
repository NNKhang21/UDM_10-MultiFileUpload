using UDM_10.Shared.Protocol;

namespace UDM_10.Shared.Models
{
    // NOTE (fix): file này trước đó trống 0 byte. Bản tối thiểu dưới đây đủ để
    // ClientSession.cs (Trần Hữu Nam) build và chạy được luồng
    // UploadStart -> Ack -> Chunk -> Ack -> Done -> Result.
    // Nga (Protocol) nên rà lại field theo đúng thiết kế cuối cùng của nhóm.

    public abstract class MessageBase
    {
        public MessageType Type { get; set; }
    }

    public class UploadStartMessage : MessageBase
    {
        public UploadStartMessage() { Type = MessageType.UploadStart; }

        // Mã định danh để phân biệt các lượt upload chạy song song.
        public string TransferId { get; set; } = string.Empty;

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