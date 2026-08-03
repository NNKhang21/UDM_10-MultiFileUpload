using System.ComponentModel;
using UDM_10.Client.Models;

namespace UDM_10.Client.Services;

public class UploadManager
{
    public BindingList<FileUploadItem> Files { get; } = new();

    // TODO: doc tu appsettings.json khi Cam Tien hoan thien ClientConfig - tam hard-code
    private const int MaxFiles = 20;
    private const int MaxFileSizeMb = 100;

    // TODO: UploadInBatchesAsync() - dieu phoi upload nhieu file dong thoi
    // TODO: CancelUpload(FileUploadItem item)
    // TODO: ResetForRetry da nam san trong FileUploadItem, chi can goi lai o day

    public bool AddFile(string path, out string? error)
    {
        if (Files.Count >= MaxFiles)
        {
            error = $"Đã đạt tối đa {MaxFiles} tệp";
            return false;
        }

        if (Directory.Exists(path))
        {
            error = $"{Path.GetFileName(path)}: Đây là thư mục, không phải file";
            return false;
        }

        if (Files.Any(f => f.FilePath == path))
        {
            error = $"{Path.GetFileName(path)}: File đã có trong danh sách";
            return false;
        }

        FileInfo info;
        try
        {
            info = new FileInfo(path);
            if (!info.Exists) throw new FileNotFoundException("File không tồn tại", path);
        }
        catch (Exception ex)
        {
            error = $"{Path.GetFileName(path)}: {ex.Message}";
            return false;
        }

        if (info.Length <= 0)
        {
            error = $"{info.Name}: File rỗng hoặc không hợp lệ";
            return false;
        }

        var maxBytes = (long)MaxFileSizeMb * 1024L * 1024L;
        if (info.Length > maxBytes)
        {
            error = $"{info.Name}: Vượt quá kích thước tối đa {MaxFileSizeMb} MB";
            return false;
        }

        Files.Add(new FileUploadItem
        {
            FileName = info.Name,
            FilePath = path,
            FileSizeBytes = info.Length
        });
        error = null;
        return true;
    }
}