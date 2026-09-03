# Startup Cleanup Checklist
Checklist này dùng để kiểm tra các yêu cầu bắt buộc của `StartupCleanupService.cs` (Server) project UDM_10.

## Checklist Startup Cleanup

| # | Yêu cầu | Ý nghĩa | Trạng thái |
|---|---|---|---|
| 1 | Kiểm tra thư mục upload có tồn tại trước khi quét | Tự tạo nếu chưa có, tránh lỗi lần chạy đầu | Đã làm |
| 2 | Quét toàn bộ file `.part` còn sót lại | Phát hiện các file `.part` còn tồn tại từ các lần upload trước để thực hiện cleanup | Đã làm |
| 3 | Xóa từng file `.part` mồ côi | Loại bỏ dữ liệu upload chưa hoàn chỉnh còn sót lại sau khi Server khởi động | Đã làm |
| 4 | Không dừng chương trình nếu một file bị khóa | Ghi log cảnh báo và tiếp tục xử lý các file còn lại | Đã làm |
| 5 | Ghi log mỗi lần xóa thành công | Ghi nhận file hoặc thư mục đã được cleanup để phục vụ kiểm tra và nghiệm thu | Đã làm |
| 6 | Chạy trước khi Server bắt đầu nhận kết nối mới | Startup cleanup phải được gọi trước listener/accept loop | Đã làm |

## Ghi chú

- Checklist dùng cho nghiệm thu Startup Cleanup.
- Mục 1–6 đã được thể hiện trực tiếp trong StartupCleanupService.cs.
- TC_39 trong bộ test hiện tại cung cấp minh chứng cho việc cleanup file `.part` mồ côi và thư mục rỗng khi Server khởi động.
- Không lặp lại nội dung Storage & Validate.