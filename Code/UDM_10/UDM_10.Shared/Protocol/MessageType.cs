namespace UDM_10.Shared.Protocol
{
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
