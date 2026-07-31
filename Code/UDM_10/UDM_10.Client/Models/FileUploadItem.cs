using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace UDM_10.Client.Models;

// Se bo sung logic tinh SentBytes/ProgressPercent o Tuan 2-3
public class FileUploadItem : INotifyPropertyChanged
{
    public string FileName { get; set; } = "";
    public string FilePath { get; set; } = "";
    public long FileSizeBytes { get; set; }

    private UploadStatus _status = UploadStatus.Waiting;
    public UploadStatus Status
    {
        get => _status;
        set { _status = value; OnChanged(); }
    }

    private double _progressPercent;
    public double ProgressPercent
    {
        get => _progressPercent;
        set { _progressPercent = value; OnChanged(); }
    }

    private long _sentBytes;
    public long SentBytes
    {
        get => _sentBytes;
        set { _sentBytes = value; OnChanged(); }
    }

    private string _speedText = "";
    public string SpeedText
    {
        get => _speedText;
        set { _speedText = value; OnChanged(); }
    }

    private string? _errorMessage;
    public string? ErrorMessage
    {
        get => _errorMessage;
        set { _errorMessage = value; OnChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}