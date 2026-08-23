namespace UDM_10.Shared.Protocol
{
    // NOTE (fix): file này trước đó trống 0 byte -> ClientSession.cs không build được.
    // Đây là bản tối thiểu để cả nhóm build/chạy thử được; Nga (Protocol) nên rà lại tên
    // và bổ sung nếu cần (ví dụ mở rộng cho multi-file: field TransferId,...).
    public enum MessageType
    {
        UploadStart,
        UploadStartAck,
        UploadChunk,
        UploadChunkAck,
        UploadDone,
        UploadResult
    }
}
