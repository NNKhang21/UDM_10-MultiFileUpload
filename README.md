# UDM_10-Upload-nhiều-file

## Thông tin đề tài
- **Mã đề tài:** UDM_10
- **Tên đề tài:** Upload nhiều file
- **Ngôn ngữ:** C# (.NET 10)
- **IDE:** Visual Studio 2026

## Thành viên nhóm
| MSSV | Họ tên | GitHub | Phần việc |
|------|--------|--------|-----------|
| |Trần Thị Thúy Nga | | Shared - Protocol |
| |Nguyễn Thị Cẩm Tiên | | Shared - Config & Log |
| |Trần Hữu Nam | | Server - Core |
| |Nguyễn Trần Tiến | | Server - Storage & Validate |
| |Nguyễn Nguyên Khang | | Client - GUI |
| |Hồ Thiên Phú | | Client - Network & Queue |

> ⚠️ Cần điền đầy đủ MSSV và link GitHub từng thành viên trước khi nộp.

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
- Không hỗ trợ: Pause/Resume, upload thư mục, Web App

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
Chi tiết đầy đủ trong `Extra/test-results/functional-test-matrix.md`. Tóm tắt:

| Loại test | Số case | Pass | Fail |
|-----------|---------|------|------|
| Functional | 10 | | |
| Negative | 6 | | |
| Disconnect | 3 | | |
| Stress (Light/Heavy) | 2 | | |

### Stress test (điền số đo thật — xem `Extra/scripts/Run-StressTest.md`)
| Chỉ số | Light | Heavy |
|--------|-------|-------|
| Tổng dữ liệu | | |
| Thời gian hoàn tất | | |
| Throughput trung bình | | |
| CPU Server (peak) | | |
| RAM Server | | |
| Tỷ lệ lỗi | | |

**Cấu hình máy test:** CPU ..., RAM ..., OS ...

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

```

## Lịch sử commit
Repository: (điền link GitHub)
