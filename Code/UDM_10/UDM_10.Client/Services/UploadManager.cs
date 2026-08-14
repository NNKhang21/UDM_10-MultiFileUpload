using System.ComponentModel;
using UDM_10.Client.Models;
using System.IO;
namespace UDM_10.Client.Services;

public record UploadOutcome(
    bool Success, 
    string? ServerFileName, 
    string? Message);
public interface IFileUploader
{
    Task<UploadOutcome> UploadFileAsync(
        string filePath, 
        IProgress<double> progress, 
        CancellationToken ct);
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
    public void CancelUpload(FileUploadItem item)
    {
        if (item.Status != UploadStatus.Uploading) return;
        item.Cts?.Cancel();
    }
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
            item.Cts?.Dispose();
            item.Cts = new CancellationTokenSource();

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            long lastBytes = 0;
            double lastSeconds = 0;

            var progress = new Progress<double>(p =>
            {
                item.ProgressPercent = p;
                long currentBytes = (long)(item.FileSizeBytes * (p / 100.0));
                item.SentBytes = currentBytes;

                double nowSeconds = stopwatch.Elapsed.TotalSeconds;
                double deltaSeconds = nowSeconds - lastSeconds;
                long deltaBytes = currentBytes - lastBytes;
                if (deltaSeconds > 0)
                {
                    double bytesPerSecond = deltaBytes / deltaSeconds;
                    item.SpeedText = FileUploadItem.FormatBytes((long)bytesPerSecond) + "/s";
                }
                lastBytes = currentBytes;
                lastSeconds = nowSeconds;
            });

            try
            {
                var result = await _uploader.UploadFileAsync(item.FilePath, progress, item.Cts.Token);
                item.ServerFileName = result.ServerFileName;
                item.Status = result.Success ? UploadStatus.Completed : UploadStatus.Failed;
                if (!result.Success) item.ErrorMessage = result.Message;
            }
            catch (OperationCanceledException)
            {
                item.Status = UploadStatus.Cancelled;
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