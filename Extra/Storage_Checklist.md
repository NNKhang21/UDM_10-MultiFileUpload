# Storage & Validate Checklist
Checklist này dùng để kiểm tra các yêu cầu bắt buộc của phần Storage & Validate (Server) project UDM_10. Nội dung chia theo 3 bảng ứng với từng nhóm chức năng của `FileStorageService.cs`.

## 1. Validate đầu vào trước khi nhận file

| # | Yêu cầu | Ý nghĩa | Trạng thái |
|---|---|---|---|
| 1 | Chặn tên file rỗng/khoảng trắng | Không tạo file nếu Client gửi tên rỗng hoặc toàn khoảng trắng | Đã làm |
| 2 | Chặn path traversal | Không cho tên chứa `..` hoặc là đường dẫn tuyệt đối, tránh ghi ra ngoài thư mục upload | Đã làm |
| 3 | Chặn ký tự không hợp lệ trong tên file | Từ chối tên chứa ký tự hệ điều hành không cho phép | Đã làm |
| 4 | Giới hạn dung lượng file | File vượt quá dung lượng tối đa cho phép phải bị từ chối | Đã làm |

## 2. Xử lý trùng tên & tranh chấp khi upload đồng thời

| # | Yêu cầu | Ý nghĩa | Trạng thái |
|---|---|---|---|
| 5 | Quy tắc xử lý trùng tên | Ghi đè, từ chối, hoặc tự đổi tên, tuỳ cấu hình đã chọn | Đã làm |
| 6 | Không lỗi khi 2 file trùng tên upload cùng lúc | 2 upload trùng tên chạy song song không được cùng chọn trúng 1 tên đích | Đã làm |

## 3. Xử lý sự cố giữa chừng & giới hạn tài nguyên

| # | Yêu cầu | Ý nghĩa | Trạng thái |
|---|---|---|---|
| 7 | Đối chiếu size thực nhận với size đã báo | Sai lệch phải báo lỗi và xoá phần dữ liệu dở dang | Chưa làm |
| 8 | Chặn Client khai sai kích thước từng phần dữ liệu | Tránh Server bị ép cấp phát bộ nhớ dư thừa | Chưa làm |
| 9 | Dọn dữ liệu dở dang khi mất kết nối giữa chừng | File chưa truyền xong không được để sót lại trên Server | Chưa làm |
| 10 | Có thời gian chờ tối đa | Client không gửi gì trong thời gian dài phải bị chủ động ngắt, không treo Server | Chưa làm |
| 11 | Ghi log khi từ chối 1 file | Lưu lại lý do để phục vụ đối chiếu khi kiểm thử | Một phần gồm: đã log ở ValidateFileName/ValidateFileSize/ResolveFinalPath; log lúc nhận file: Week 4  |
| 12 | Chặn tên thiết bị dành riêng của Windows | CON, PRN, AUX, NUL, COM1-9, LPT1-9 đều không nằm trong danh sách ký tự cấm nhưng vẫn bị hệ điều hành từ chối | Đã làm |
## Ghi chú
- Đây là checklist dùng để kiểm tra phần Storage & Validate trước khi nghiệm thu
- Mỗi mục sau khi hoàn thành nên được kiểm thử và đánh dấu đạt
- Checklist này chỉ mô tả yêu cầu Storage & Validate, không mô tả chi tiết phần xử lý của GUI
