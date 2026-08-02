# BÁO CÁO TUẦN 1 - TRẦN THỊ THÚY NGA

## 1. Module phụ trách

Protocol dùng chung giữa Client và Server.

Các file theo kế hoạch:

- Shared/Protocol/MessageType.cs
- Shared/Protocol/MessageFramer.cs
- Shared/Models/Messages.cs

Các hàm cần tìm hiểu:

- WriteAsync
- ReadJsonAsync
- ReadRawAsync

## 2. Môi trường đã chuẩn bị

- Đã cài Git.
- Đã cài .NET SDK 10.
- Đã clone repository của nhóm.
- Đã tạo nhánh dev/nga-protocol.
- Đã build scaffold thành công.

## 3. Cấu trúc frame cần tìm hiểu

Một frame dự kiến có cấu trúc:

[4 byte độ dài] + [nội dung JSON hoặc dữ liệu]

Bốn byte đầu cho biết số byte của phần dữ liệu phía sau.

TCP có thể không trả đủ dữ liệu trong một lần đọc, vì vậy cần đọc lặp
cho tới khi nhận đủ số byte yêu cầu.

## 4. Những chỗ dự kiến cần mở rộng

- Xử lý partial read.
- Kiểm tra độ dài frame hợp lệ.
- Báo lỗi khi kết nối đóng giữa chừng.
- Có thể bổ sung transferId để phân biệt nhiều file đang upload.
- Hỗ trợ timeout và CancellationToken.

## 5. Vướng mắc hiện tại

Trên nhánh main hiện chưa có các file MessageType.cs,
MessageFramer.cs và Messages.cs nên chưa thể kiểm tra implementation
của WriteAsync, ReadJsonAsync và ReadRawAsync.

Cần trưởng nhóm bổ sung scaffold Shared/Protocol hoặc xác nhận người
phụ trách tự tạo project Shared.