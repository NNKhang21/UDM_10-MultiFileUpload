namespace UDM_10.Shared.Protocol;

using System.IO;

public static class ServerEvent
{
    // viêt in hoa cho de nhin 
    public const string ServerStart = "SERVER_START";
    public const string Connect = "CONNECT";
    public const string Disconnect = "DISCONNECT";
    public const string UploadStart = "UPLOAD_START";
    public const string UploadAck = "UPLOAD_ACK";
    public const string UploadComplete = "UPLOAD_COMPLETE";
    public const string UploadIncomplete = "UPLOAD_INCOMPLETE";
    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string ProtocolError = "PROTOCOL_ERROR";
    public const string Cleanup = "CLEANUP";
    public const string IdleTimeout = "IDLE_TIMEOUT";
}
