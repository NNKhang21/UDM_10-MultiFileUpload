using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace UDM_10.Client.Models;
// Se bo sung logic tinh SentBytes/ProgressPercent day du hon o Tuan 3
public class FileUploadItem : INotifyPropertyChanged
{
    public string FileName { get; set; } = "";
    public string FilePath { get; set; } = "";
    public long FileSizeBytes { get; set; }

    // CTS rieng cho tung file: cho phep Cancel 1 file ma khong anh huong
    // cac file khac dang upload dong thoi. Tao moi moi lan bat dau upload.
    public CancellationTokenSource? Cts { get; set; }

    // Hien "12.0 MB" thay vi so byte tho, dung o cot Size cua DataGridView
    public string FileSizeText => FormatBytes(FileSizeBytes);

    private UploadStatus _status = UploadStatus.Waiting;
    public UploadStatus Status { 
        get => _status; 
        set { _status = value; OnChanged(); } 
    }
    // Hien tieng Viet cho cot "Trang thai" tren DataGridView
    public string StatusLabel => Status switch
    {
        UploadStatus.Waiting => "Đang chờ",
        UploadStatus.Uploading => "Đang tải lên",
        UploadStatus.Completed => "Hoàn thành",
        UploadStatus.Failed => "Lỗi",
        UploadStatus.Cancelled => "Đã hủy",
        _ => Status.ToString()
    };
    private double _progressPercent;
    public double ProgressPercent { 
        get => _progressPercent; 
        set { _progressPercent = value; OnChanged(); OnChanged(nameof(ProgressPercentText)); } 
    }

    private long _sentBytes;
    public long SentBytes {
        get => _sentBytes; 
        set { _sentBytes = value; OnChanged(); OnChanged(nameof(ProgressText)); } 
    }

    // Hien "14.2 MB / 20 MB" o duoi progress bar
    public string ProgressText => $"{FormatBytes(SentBytes)} / {FormatBytes(FileSizeBytes)}";

    private string _speedText = "";
    public string SpeedText { get => _speedText; set { _speedText = value; OnChanged(); } }

    private string? _errorMessage;
    public string? ErrorMessage { get => _errorMessage; set { _errorMessage = value; OnChanged(); } }

    public string ProgressPercentText => Status == UploadStatus.Uploading || Status == UploadStatus.Completed
    ? $"{ProgressPercent:0}%"
    : "";

    // Dung cho Retry: dua file ve trang thai ban dau truoc khi day lai vao hang doi
    public void ResetForRetry()
    {
        Status = UploadStatus.Waiting;
        ProgressPercent = 0;
        SentBytes = 0;
        SpeedText = "";
        ErrorMessage = null;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public static string FormatBytes(long bytes)
    {
        double mb = bytes / 1024.0 / 1024.0;
        if (mb >= 1) return $"{mb:0.0} MB";
        return $"{bytes / 1024.0:0.0} KB";
    }
}