using System;

using UDM_10.Shared.Protocol;

namespace UDM_10.Shared.Models

{

    // Base class dùng chung cho tất cả message trao đổi

    // giữa Client và Server.

    public abstract class MessageBase

    {

        public MessageType Type { get; set; }

    }

    // Client gửi yêu cầu bắt đầu upload file

    public class UploadStartMessage : MessageBase

    {

        public UploadStartMessage()

        {

            Type = MessageType.UploadStart;

        }

        // ID riêng cho từng lượt upload.

        // Dùng để phân biệt nhiều file upload cùng lúc.

        public string TransferId { get; set; } = string.Empty;

        public string FileName { get; set; } = string.Empty;

        public long FileSize { get; set; }

    }

    // Client gửi từng phần dữ liệu của file

    public class UploadChunkMessage : MessageBase

    {

        public UploadChunkMessage()

        {

            Type = MessageType.UploadChunk;

        }

        // Xác định chunk thuộc file upload nào

        public string TransferId { get; set; } = string.Empty;

        // Thứ tự chunk

        public int ChunkIndex { get; set; }

        // Số byte dữ liệu thực tế trong chunk

        public int DataLength { get; set; }

        // Dữ liệu file được encode Base64

        public string DataBase64 { get; set; } = string.Empty;

    }

    // Client báo đã gửi xong file

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

        // Constructor mặc định phục vụ deserialize JSON

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

            // Dùng UTC để tránh lỗi múi giờ

            Timestamp = DateTime.UtcNow;

        }

        // Đối chiếu ACK với đúng upload

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

        public string TransferId { get; set; } = string.Empty;

        public bool IsSuccess { get; set; }
       public string Message { get; set; } = string.Empty;

    }

    // Message báo lỗi protocol/network

    // Dùng cho Tuần 4

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