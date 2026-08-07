using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UDM_10.Shared.Config;
using UDM_10.Shared.Models;

namespace UDM_10.Server
{
    // NOTE (fix) - đây là bản TẠM để build/chạy được luồng upload 1 file (mục tiêu Tuần 2).
    // Trần Tiến (chủ file) nên rà lại toàn bộ phần TODO/validate cho đúng yêu cầu đầy đủ
    // (chặn path traversal kỹ hơn, DuplicatePolicy Reject/Overwrite/Rename cấu hình được, v.v.)
    //
    // Các thay đổi so với bản gốc:
    //  1. Constructor cũ nhận "ServerConfig config" (instance) nhưng ServerConfig là static class
    //     -> không compile được. Đã đổi thành constructor không tham số, đọc thẳng ServerConfig.* (static).
    //  2. ClientSession gọi _storage.BeginUploadAsync/WriteChunkAsync/FinishUploadAsync nhưng file này
    //     trước đó không có 3 hàm đó (chỉ có PrepareUploadAsync/ReceiveFileAsync...) -> đã thêm 3 hàm
    //     public làm "orchestrator" gọi lại các hàm nội bộ bên dưới.
    //  3. Thiếu "using System;" và "using System.Threading.Tasks;" -> lỗi CS0246/CS0103.
    public class FileStorageService
    {
        private const int MaxFileNameLength = 255;

        private static readonly string[] _windowsReservedNames =
        {
            "CON", "PRN", "AUX", "NUL",
            "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
            "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
        };

        private static readonly SemaphoreSlim _nameLock = new(1, 1);

        // Trạng thái của lượt upload hiện tại trên session này (1 FileStorageService / 1 ClientSession)
        private string _targetPath = string.Empty;
        private FileStream? _partFile;
        private long _expectedSize;
        private long _receivedBytes;

        public FileStorageService()
        {
            Directory.CreateDirectory(ServerConfig.UploadDirectory);
        }

        #region Orchestration (gọi từ ClientSession)

        public async Task BeginUploadAsync(UploadStartMessage start)
        {
            ValidateFileName(start.FileName);
            ValidateFileSize(start.FileSize);

            var (targetPath, partFile) = await PrepareUploadAsync(start.FileName);
            _targetPath = targetPath;
            _partFile = partFile;
            _expectedSize = start.FileSize;
            _receivedBytes = 0;
        }

        public async Task WriteChunkAsync(UploadChunkMessage chunk)
        {
            if (_partFile == null)
            {
                throw new InvalidOperationException("Chưa nhận UploadStart trước khi nhận Chunk.");
            }

            byte[] data = Convert.FromBase64String(chunk.DataBase64);
            ValidateChunkLength(data.Length, _expectedSize - _receivedBytes);

            await WriteChunkToPartFile(_partFile, data, default);
            _receivedBytes += data.Length;
        }

        public async Task<bool> FinishUploadAsync(UploadDoneMessage done)
        {
            try
            {
                VerifyUpload(_receivedBytes, _expectedSize);

                if (_partFile != null)
                {
                    await _partFile.FlushAsync();
                    _partFile.Close();
                }

                CompleteUpload(_targetPath);
                return true;
            }
            catch (Exception)
            {
                RollbackUpload(_targetPath);
                return false;
            }
        }

        #endregion

        #region Validation

        public void ValidateFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("Tên file rỗng.");

            if (fileName.Length > MaxFileNameLength)
                throw new ArgumentException("Tên file quá dài.");

            if (fileName.Contains("..") || fileName.Contains('/') || fileName.Contains('\\'))
                throw new ArgumentException("Tên file chứa ký tự path traversal không hợp lệ.");

            string nameOnly = Path.GetFileNameWithoutExtension(fileName).ToUpperInvariant();
            foreach (var reserved in _windowsReservedNames)
            {
                if (nameOnly == reserved)
                    throw new ArgumentException($"Tên file trùng tên dành riêng của Windows: {reserved}");
            }
        }

        public void ValidateFileSize(long fileSize)
        {
            if (fileSize <= 0)
                throw new ArgumentException("Kích thước file không hợp lệ.");

            long maxBytes = ServerConfig.MaxFileSizeMb * 1024 * 1024;
            if (fileSize > maxBytes)
                throw new ArgumentException($"File vượt quá giới hạn {ServerConfig.MaxFileSizeMb}MB.");
        }

        public void ValidateChunkLength(long declaredLength, long remainingBytes)
        {
            if (declaredLength <= 0)
                throw new ArgumentException("Chunk rỗng hoặc không hợp lệ.");

            if (declaredLength > remainingBytes)
                throw new ArgumentException("Chunk vượt quá dung lượng còn lại của file.");
        }

        #endregion

        #region Upload Preparation

        public string GetUploadPath(string fileName)
        {
            string targetPath = Path.Combine(ServerConfig.UploadDirectory, fileName);

            // Chính sách tạm: Rename nếu trùng tên (Tiến chỉnh lại nếu cần Reject/Overwrite theo config)
            if (File.Exists(targetPath) || File.Exists(targetPath + ".part"))
            {
                targetPath = GenerateDuplicateName(targetPath);
            }

            return targetPath;
        }

        public async Task<(string TargetPath, FileStream PartFile)> PrepareUploadAsync(string fileName, CancellationToken ct = default)
        {
            await _nameLock.WaitAsync(ct);
            try
            {
                string targetPath = GetUploadPath(fileName);
                string partPath = targetPath + ".part";
                FileStream partFile = new FileStream(partPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
                return (targetPath, partFile);
            }
            finally
            {
                _nameLock.Release();
            }
        }

        private static string GenerateDuplicateName(string targetPath)
        {
            string dir = Path.GetDirectoryName(targetPath) ?? string.Empty;
            string name = Path.GetFileNameWithoutExtension(targetPath);
            string ext = Path.GetExtension(targetPath);

            int i = 1;
            string candidate;
            do
            {
                candidate = Path.Combine(dir, $"{name}({i}){ext}");
                i++;
            } while (File.Exists(candidate) || File.Exists(candidate + ".part"));

            return candidate;
        }

        #endregion

        #region Upload Process

        private async Task WriteChunkToPartFile(FileStream partFile, byte[] buffer, CancellationToken ct)
        {
            await partFile.WriteAsync(buffer, ct);
        }

        private void VerifyUpload(long receivedSize, long expectedSize)
        {
            if (receivedSize != expectedSize)
                throw new InvalidDataException($"Dữ liệu nhận được ({receivedSize}) không khớp kích thước khai báo ({expectedSize}).");
        }

        private void CompleteUpload(string targetPath)
        {
            string partPath = targetPath + ".part";
            File.Move(partPath, targetPath, overwrite: true);
        }

        #endregion

        #region Cleanup

        public void RollbackUpload(string targetPath)
        {
            DeletePartialFile(targetPath);
        }

        public bool DeletePartialFile(string filePath)
        {
            try
            {
                string partPath = filePath + ".part";
                if (File.Exists(partPath))
                {
                    _partFile?.Close();
                    File.Delete(partPath);
                    return true;
                }
                return false;
            }
            catch (IOException)
            {
                // File đang bị khoá, bỏ qua thay vì crash
                return false;
            }
        }

        #endregion
    }
}
