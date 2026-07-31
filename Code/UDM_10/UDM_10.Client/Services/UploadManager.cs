using System.ComponentModel;
using UDM_10.Client.Models;

namespace UDM_10.Client.Services;

public class UploadManager
{
    public BindingList<FileUploadItem> Files { get; } = new();

    // TODO: AddFile(string path) - them file vao Files, check trung duong dan
    // TODO: UploadInBatchesAsync() - dieu phoi upload nhieu file dong thoi bang SemaphoreSlim
    // TODO: CancelUpload(FileUploadItem item) - huy 1 file rieng le
    // TODO: ResetForRetry(FileUploadItem item) - reset file Failed/Cancelled ve Waiting
}