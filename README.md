# UDM_10-Upload-nhiều-file

## Thông tin đề tài
- **Mã đề tài:** UDM_10
- **Tên đề tài:** Upload nhiều file
- **Ngôn ngữ:** C# (.NET 10)
- **IDE:** Visual Studio 2026

## Thành viên nhóm
| MSSV | Họ tên | GitHub | Phần việc |
|------|--------|--------|-----------|
| 052306010855 | Trần Thị Thúy Nga | Thngatran | Shared - Protocol |
| 072306011739 | Nguyễn Thị Cẩm Tiên | camtien7426-cmd
camtien7426-cmd | Shared - Config & Log |
| 077206010411 | Trần Hữu Nam | namth0411-netizen | Server - Core |
| 064206000683 | Nguyễn Trần Tiến | tien230526 | Server - Storage & Validate |
| 087206005109 | Nguyễn Nguyên Khang | NNKhang21
NNKhang21 | Client - GUI |
| 052205010238 | Hồ Thiên Phú | hothienphu | Client - Network & Queue |


## Kiến trúc
- Mô hình: **Client–Server**
- Client: **WinForms** (.NET 10, `net10.0-windows`), chạy như 1 tiến trình riêng
- Server: Console App (.NET 10), `TcpListener`, mỗi kết nối chạy trên 1 Task riêng
- Shared: thư viện dùng chung cho protocol, model, cấu hình

```
Client (WinForms) ──TCP──> Server (Console)
     │                      │
  UploadQueue          ClientSession (1/kết nối)
  NetworkClient        FileStorageService
     │                      │
     └────── UDM_10.Shared ─┘
        (MessageFramer, Models, Config)
```

**Lưu ý thiết kế quan trọng:** mỗi file upload mở **một kết nối TCP riêng** (không dùng chung 1 socket cho nhiều file cùng lúc), để tránh xung đột khi nhiều upload chạy song song. Số kết nối đồng thời bị giới hạn bởi `MaxConcurrentUploads` ở phía Client.

## Protocol
- **Transport:** TCP
- **Port mặc định:** 9000 (Server đọc từ `appsettings.json`, không hard-code)
- **Framing:** `[4 byte length, big-endian][UTF-8 JSON payload]`
- **Message types:** `UploadStart → UploadStartAck → UploadChunk (nhiều lần) → UploadDone → UploadResult`, hoặc `Error`

## Giới hạn sản phẩm (công bố rõ)
- Upload đồng thời tối đa: **5** (tham khảo chuẩn thực tế: Chrome/Firefox giới hạn 6 kết nối đồng thời/host, các thư viện upload phổ biến như Uppy/Dropzone.js mặc định 3–5 file song song; chọn 5 để demo thấy rõ nhiều file chạy song song mà không gây tranh chấp I/O đĩa khi Server ghi nhiều file cùng lúc trên 1 máy)
- Kích thước file tối đa: **150 MB** (Server validate, cấu hình qua `appsettings.json`)
- Chính sách trùng tên: `DuplicatePolicy` — mặc định **Rename** (`file(1).ext`)
- Không triển khai (theo phạm vi đề tài UDM_10 và ràng buộc chung của môn): Pause/Resume (tránh trùng phạm vi với UDM_12), upload thư mục, Web App (đề bài không cho phép)

## Yêu cầu môi trường
- Windows 10/11
- .NET 10 SDK
- Visual Studio 2026 (có workload ".NET desktop development")

## Cấu hình
### Server (`Code/UDM_10.Server/appsettings.json`)
File này **được Server đọc thật** lúc khởi động (`ServerConfig.Load`).

| Key | Mô tả | Mặc định |
|-----|-------|----------|
| Host | IP bind | 0.0.0.0 |
| Port | Port lắng nghe | 9000 |
| UploadDirectory | Thư mục lưu file | ./uploads |
| MaxFileSizeMb | Giới hạn file | 150 |
| ChunkSizeKb | Dùng để Server validate kích thước mỗi chunk nhận từ Client | 64 |
| DuplicatePolicy | Rename / Overwrite / Reject | Rename |
| IdleTimeoutSeconds | Số giây tối đa chờ dữ liệu mới từ 1 Client (frame điều khiển lẫn từng chunk) trước khi coi là "treo" và chủ động đóng kết nối | 30 |

### Client
Client **hiện chưa đọc file `appsettings.json`** — file này tồn tại trên đĩa nhưng chưa được wire vào code (xem mục Hạn chế bên dưới). Tham số mạng của Client hiện tại:

| Tham số | Cách cấu hình thật | Giá trị mặc định |
|---------|--------------------|--------------------|
| IP / Port Server | Nhập trực tiếp qua ô nhập trên GUI lúc chạy (không hard-code 1 máy cố định) | 127.0.0.1 : 9000 |
| MaxConcurrentUploads | Hằng số trong `UploadManager.cs` | 5 |
| ChunkSizeKb | Hằng số trong `NetworkClient.cs` | 64 |
| ConnectTimeoutMs | Hằng số trong `NetworkClient.cs` | 5000 |
| IdleTimeout (chờ Ack/Result từ Server) | Hằng số trong `NetworkClient.cs` | 30000 (30s) |

## Hướng dẫn chạy
1. Mở `Code/UDM_10.sln` trong Visual Studio.
2. Set Startup Project = **UDM_10.Server** → F5 (giữ cửa sổ console đang chạy).
3. Set Startup Project = **UDM_10.Client** → chạy thêm 1 instance (Debug → Start New Instance, hoặc chạy .exe trong `bin/Debug`).
4. Trên Client: nhập IP/Port → **Connect** → kéo thả file hoặc **Chọn file...** → **Upload tất cả**.
5. Kiểm tra file đã lưu trong `Code/UDM_10.Server/uploads/`.

## Kiểm thử
Chi tiết đầy đủ từng test case (mô tả, input, kết quả, ảnh minh chứng) trong `Extra/Test_Cases_UDM_10.xlsx`, sheet "Danh Sách Test_Case" + 56 sheet con TC_01 → TC_56.

| Module | Số case | Pass | Fail |
|---|---|---|---|
| Client GUI | 19 | 19 | 0 |
| Config | 3 | 3 | 0 |
| Logger | 2 | 2 | 0 |
| Protocol | 6 | 6 | 0 |
| Storage & Validate | 9 | 9 | 0 |
| Server Core | 7 | 7 | 0 |
| Network & Queue | 5 | 5 | 0 |
| Tích hợp (End-to-end) | 4 | 4 | 0 |
| Build (toàn Solution) | 1 | 1 | 0 |
| **Tổng** | **56** | **56** | **0** |

Bao gồm đủ các loại theo yêu cầu môn học:
- **Functional**: toàn bộ chức năng bắt buộc (kéo-thả, progress/speed riêng từng file, giới hạn upload đồng thời, xử lý trùng tên...).
- **Negative / dữ liệu không hợp lệ**: TC_27–TC_38 (path traversal, tên file cấm của Windows, sai kích thước/chunk, sai thứ tự chunk...).
- **Mất kết nối / ngắt đột ngột**: TC_13, TC_30, TC_41, TC_42, TC_46, TC_54 (rút mạng giữa chừng, tắt Server đột ngột, idle timeout, Ctrl+C shutdown).
- **Stress / performance – 2 mức tải**: TC_55 (Mức 1: 12 file / 54,9 MB; Mức 2: 13 file / 613,5 MB), theo dõi CPU/RAM qua Task Manager, xác minh toàn vẹn dữ liệu bằng hash SHA-256 (TC_49, TC_52).

**Cấu hình máy test:** CPU ..., RAM ..., OS ... *(điền thông tin máy thật đã dùng để test)*

> Ghi chú rà soát trước khi nộp: trong quá trình tổng hợp lại bảng test case, nhóm đã phát hiện và xử lý 3 điểm chưa nhất quán giữa mô tả kết quả và kết luận Pass/Fail (TC_08 — hành vi Overwrite/Rename; TC_21 — thiếu ảnh log minh chứng; TC_44/TC_45 — ảnh minh chứng bị đảo chỗ), cùng một cột dữ liệu nháp còn sót lại trong sheet tổng hợp. Toàn bộ đã được kiểm tra và cập nhật lại cho khớp với kết quả chạy thực tế; số liệu 56/56 PASS ở trên là số liệu sau khi rà soát.

## Video demo
- **Link:** (điền sau khi quay)
- Nội dung: connect, multi-upload, progress/speed, trùng tên, lỗi/disconnect, stress ngắn — mỗi thành viên trình bày phần mình, có hiện mặt.

## Hạn chế và phần chưa hoàn thành
- Chưa hỗ trợ Pause/Resume
- Chưa test trên LAN 2 máy thật (mới test qua localhost)
- Client chưa đọc `appsettings.json` (class `ClientConfig` đã có sẵn trong `UDM_10.Shared` nhưng chưa được gọi `Load()`); tham số Client hiện đang hard-code hằng số trong code, chỉ IP/Port là cấu hình được thật qua GUI lúc chạy
- (Bổ sung thêm khi làm thực tế)

## Cấu trúc repository
```
UDM_10_MultiFileUpload/
├── README.md
├── .gitignore
├── Code/
│   ├── UDM_10.sln
│   ├── UDM_10.Shared/
│   ├── UDM_10.Server/
│   └── UDM_10.Client/
├── DOCX/
├── PPTX/
└── Extra/
    └── Test_Cases_UDM_10.xlsx
```

## Lịch sử commit
Repository: https://github.com/NNKhang21/UDM_10-MultiFileUpload
