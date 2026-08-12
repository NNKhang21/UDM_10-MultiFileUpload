# GUI Checklist

Tài liệu này dùng để kiểm tra các yêu cầu của giao diện (GUI) cho Client trong project **UDM_10 - Multi File Upload**.

## Checklist GUI (12 mục)

| # | Yêu cầu | Ý nghĩa với GUI |
|---|---|---|
| 1 | Kết nối/ngắt kết nối Server | Nút **Connect/Disconnect**, hiển thị rõ trạng thái **"Đã kết nối"** / **"Đã ngắt kết nối"**. |
| 2 | Kéo-thả 1 hoặc nhiều file | Drop 1 file → thêm 1 dòng; chọn nhiều file qua File Dialog → thêm đủ dòng tương ứng. |
| 3 | Mỗi file có trạng thái riêng | Không dùng chung 1 trạng thái cho cả lô; mỗi dòng độc lập: **Chờ / Đang tải / Hoàn tất / Lỗi**. |
| 4 | Giới hạn upload đồng thời + công bố rõ | Tối đa **N** file được **Đang tải** cùng lúc; GUI phải hiển thị giới hạn này, các file còn lại xếp hàng chờ. |
| 5 | Progress % + tốc độ riêng từng file | Mỗi dòng hiển thị **Progress (%)** và **tốc độ (MB/s)** riêng. |
| 6 | 1 file lỗi không ảnh hưởng file khác | Chỉ file gặp lỗi chuyển sang trạng thái **Lỗi**; các file khác vẫn tiếp tục upload bình thường. |
| 7 | Xử lý trùng tên | GUI hiển thị đúng kết quả Server trả về, kể cả khi nhiều file trùng tên được upload đồng thời. |
| 8 | GUI không treo | Upload nhiều file nhưng giao diện vẫn phản hồi bình thường, người dùng vẫn thao tác được. |
| 9 | Lỗi kết nối rõ ràng | Sai IP/Port phải báo lỗi rõ ràng. Phân biệt trường hợp **Connection Refused** và **Connection Timeout**. Nếu Upload khi chưa Connect thì báo lỗi, không crash. |
| 10 | Mất kết nối giữa chừng | Nếu Server tắt trong lúc upload thì GUI phải báo lỗi rõ ràng và không bị treo hoặc crash. |
| 11 | Hủy/Thử lại từng file | Mỗi dòng file có nút **Cancel** và **Retry** riêng. |
| 12 | IP/Port cấu hình được, không hard-code | GUI phải có ô nhập **IP** và **Port** trước khi Connect; không được gán cứng trong source code. Có thể đổi IP/Port và kết nối lại thành công. |

---
- [x] Kéo-thả file+thư mục cùng lúc: chưa xác nhận MessageBox báo lỗi thư mục có
      hiện đúng không (test bị nhiễu do thao tác tay, cần debug lại kỹ hơn sau)
- [x] Giới hạn upload đồng thời + công bố rõ — MaxConcurrentUploads = 3,
      hiện qua lblConcurrencyInfo ngay khi mở app
## Ghi chú

- Đây là checklist dùng để kiểm tra giao diện Client trước khi nghiệm thu.
- Mỗi mục sau khi hoàn thành nên được kiểm thử và đánh dấu đạt.
- Checklist này chỉ mô tả yêu cầu GUI, không mô tả chi tiết phần xử lý của Server.
