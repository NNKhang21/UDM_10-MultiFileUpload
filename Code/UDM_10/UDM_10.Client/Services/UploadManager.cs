using System.ComponentModel;
using UDM_10.Client.Models;

namespace UDM_10.Client.Services;

public interface IFileUploader
{
    Task<bool> UploadFileAsync(string filePath, IProgress<double> progress, CancellationToken ct);
}
public class UploadManager
{
    public BindingList<FileUploadItem> Files { get; } = new();

    private readonly IFileUploader _uploader;

    // TODO: doc tu appsettings.json khi Cam Tien hoan thien ClientConfig - tam hard-code
    private const int MaxFiles = 20;
    private const int MaxFileSizeMb = 100;
    public const int MaxConcurrentUploads = 3; // TODO: doc tu config khi Cam Tien bo sung field nay
    private readonly SemaphoreSlim _uploadSemaphore = new(MaxConcurrentUploads, MaxConcurrentUploads);
    // TODO: UploadInBatchesAsync() - dieu phoi upload nhieu file dong thoi
    // TODO: CancelUpload(FileUploadItem item)
    // TODO: ResetForRetry da nam san trong FileUploadItem, chi can goi lai o day
    public UploadManager(IFileUploader uploader)
    {
        _uploader = uploader;
    }
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

    public async Task UploadInBatchesAsync()
    {
        var pending = Files.Where(f => f.Status == UploadStatus.Waiting).ToList();

        var tasks = pending.Select(item => UploadOneFileAsync(item)).ToList();
        await Task.WhenAll(tasks);
    }

    private async Task UploadOneFileAsync(FileUploadItem item)
    {
        await _uploadSemaphore.WaitAsync();
        try
        {
            item.Status = UploadStatus.Uploading;
            var progress = new Progress<double>(p => item.ProgressPercent = p);
            try
            {
                bool ok = await _uploader.UploadFileAsync(item.FilePath, progress, CancellationToken.None);
                item.Status = ok ? UploadStatus.Completed : UploadStatus.Failed;
            }
            catch (Exception ex)
            {
                item.Status = UploadStatus.Failed;
                item.ErrorMessage = ex.Message;
            }
        }
        finally
        {
            _uploadSemaphore.Release();
        }
    }
}