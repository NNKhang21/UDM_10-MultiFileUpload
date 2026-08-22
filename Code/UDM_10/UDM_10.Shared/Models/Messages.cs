using System;

using UDM_10.Shared.Protocol;

namespace UDM_10.Shared.Models

{

    // Base class dùng chung cho tất cả message

    // trao đổi giữa Client và Server

    public abstract class MessageBase

    {

        public MessageType Type { get; set; }

    }

    // Client bắt đầu upload file

    public class UploadStartMessage : MessageBase

    {

        public UploadStartMessage()

        {

            Type = MessageType.UploadStart;

        }

        // ID riêng cho từng lượt upload

        public string TransferId { get; set; } = string.Empty;

        public string FileName { get; set; } = string.Empty;

        public long FileSize { get; set; }

    }

    // Client gửi từng phần dữ liệu file

    public class UploadChunkMessage : MessageBase

    {

        public UploadChunkMessage()

        {

            Type = MessageType.UploadChunk;

        }

        // Xác định chunk thuộc file upload nào

        public string TransferId { get; set; } = string.Empty;

        // Số thứ tự chunk

        public int ChunkIndex { get; set; }

        // Số byte dữ liệu thực tế

        public int DataLength { get; set; }

        // Dữ liệu file Base64

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

    // Server phản hồi ACK

    public class AckMessage : MessageBase

    {

        // Constructor cho JSON deserialize

        public AckMessage()

        {

        }

        // Tạo ACK chủ động

        public AckMessage(MessageType type)

        {

            if(type != MessageType.UploadStartAck &&

               type != MessageType.UploadChunkAck)

            {

                throw new ArgumentException(

                    "AckMessage chỉ hỗ trợ UploadStartAck hoặc UploadChunkAck.",

                    nameof(type));

            }

            Type = type;

            Timestamp = DateTime.UtcNow;

        }

        // Xác định ACK thuộc upload nào

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

        // ID của lượt upload

        public string TransferId { get; set; } = string.Empty;

        // Upload thành công hay thất bại

        public bool IsSuccess { get; set; }

        // Tên file thật trên Server

        // Ví dụ:

        // Client gửi abc.txt
        // Server đổi thành abc_1.txt

        public string ServerFileName { get; set; } = string.Empty;

        // Nội dung thông báo

        public string Message { get; set; } = string.Empty;

    }

    // Message báo lỗi Protocol / Network

    public class ErrorMessage : MessageBase

    {

        public ErrorMessage()

        {

            Type = MessageType.Error;

        }

        public string ErrorCode { get; set; } = string.Empty;

        public string ErrorMessageText { get; set; } = string.Empty;

        public string TransferId { get; set; } = string.Empty;

    }

}