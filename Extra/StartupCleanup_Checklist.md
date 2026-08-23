# Startup Cleanup Checklist
Checklist này dùng để kiểm tra các yêu cầu bắt buộc của `StartupCleanupService.cs` (Server) project UDM_10.

## Checklist Startup Cleanup (6 mục)

| # | Yêu cầu | Ý nghĩa |
|---|---|---|
| 1 | Kiểm tra thư mục upload có tồn tại trước khi quét | Tránh lỗi nếu thư mục chưa từng được tạo (lần chạy đầu tiên) |
| 2 | Quét toàn bộ file `.part` còn sót lại trong thư mục upload | Đây là dữ liệu dở dang từ lần chạy trước, không được giữ lại |
| 3 | Xoá từng file `.part` mồ côi tìm thấy | Đảm bảo không còn dữ liệu chưa hoàn chỉnh trước khi Server nhận upload mới |
| 4 | Không dừng chương trình nếu 1 file bị khoá | Ghi log cảnh báo thay vì làm crash Server |
| 5 | Ghi log mỗi lần xoá thành công | Có bằng chứng cụ thể file nào đã được dọn |
| 6 | Chạy trước khi Server bắt đầu nhận kết nối mới | Đảm bảo dọn dẹp xong hoàn toàn trước khi có Client nào kết nối vào |

## Ghi chú
- Đây là checklist dùng để kiểm tra phần Startup Cleanup trước khi nghiệm thu
- Mỗi mục sau khi hoàn thành nên được kiểm thử và đánh dấu đạt
- Checklist này chỉ mô tả yêu cầu của `StartupCleanupService.cs`, không lặp lại nội dung đã có trong `Storage_Checklist.md`
