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
    private readonly UploadQueue _uploadQueue;
    private const int MaxFiles = 50;
    private const int MaxFileSizeMb = 100;
    public const int MaxConcurrentUploads = 3; 
    public void CancelUpload(FileUploadItem item)
    {
        if (item.Status != UploadStatus.Uploading) return;
        item.Cts?.Cancel();
    }
    
    public UploadManager(IFileUploader uploader)
    {
        _uploader = uploader;
        _uploadQueue = new UploadQueue(uploader, MaxConcurrentUploads);
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
    public async Task UploadSelectedAsync()
    {
        var pending = Files.Where(f => f.IsSelected && f.Status == UploadStatus.Waiting).ToList();
        var tasks = pending.Select(item => UploadOneFileAsync(item)).ToList();
        await Task.WhenAll(tasks);
    }

    private async Task UploadOneFileAsync(FileUploadItem item)
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
            var result = await _uploadQueue.EnqueueAsync(item.FilePath, progress, item.Cts.Token);
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

    public async Task RetryUploadAsync(FileUploadItem item)
    {
        if (item.Status != UploadStatus.Failed && item.Status != UploadStatus.Cancelled) return;
        item.ResetForRetry();
        await UploadOneFileAsync(item);
    }
    public bool RemoveFile(FileUploadItem item)
    {
        if (item.Status == UploadStatus.Waiting || item.Status == UploadStatus.Uploading) return false;
        return Files.Remove(item);
    }
}