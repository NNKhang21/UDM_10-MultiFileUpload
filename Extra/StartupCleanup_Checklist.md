# Startup Cleanup Checklist
Checklist này dùng để kiểm tra các yêu cầu bắt buộc của `StartupCleanupService.cs` (Server) project UDM_10.

## Checklist Startup Cleanup

| # | Yêu cầu | Ý nghĩa | Trạng thái |
|---|---|---|---|
| 1 | Kiểm tra thư mục upload có tồn tại trước khi quét | Tự tạo nếu chưa có, tránh lỗi lần chạy đầu | Đã làm |
| 2 | Quét toàn bộ file `.part` còn sót lại | Dọn dữ liệu dở dang từ lần chạy trước | Đã làm |
| 3 | Xóa từng file `.part` mồ côi | Không để dữ liệu upload chưa hoàn chỉnh tồn tại | Đã làm |
| 4 | Không dừng chương trình nếu 1 file bị khóa | Log cảnh báo và tiếp tục file khác | Đã làm |
| 5 | Ghi log mỗi lần xóa thành công | Có bằng chứng cụ thể file nào được dọn | Đã làm |
| 6 | Chạy trước khi Server bắt đầu nhận kết nối mới | Startup cleanup phải được gọi trước listener/accept loop | Cần xác nhận ở Program |

## Ghi chú

- Checklist dùng cho nghiệm thu Startup Cleanup.
- Mục 1–5 đã được thể hiện trực tiếp trong StartupCleanupService.cs.
- Mục 6 cần kiểm tra nơi khởi động Server/Program để xác nhận thứ tự gọi.
- Không lặp lại nội dung Storage & Validate.