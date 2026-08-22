namespace UDM_10.Shared.Protocol

{

    public enum MessageType

    {

        // Client bắt đầu upload

        UploadStart,

        // Server xác nhận nhận yêu cầu

        UploadStartAck,

        // Client gửi từng phần dữ liệu

        UploadChunk,

        // Server xác nhận chunk

        UploadChunkAck,

        // Client báo gửi xong

        UploadDone,

        // Server trả kết quả cuối

        UploadResult,

        // Thông báo lỗi protocol/network

        Error

    }

}