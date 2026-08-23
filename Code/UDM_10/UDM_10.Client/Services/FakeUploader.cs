namespace UDM_10.Client.Services;

// TAM THOI - xoa khi co NetworkClient that cua Phu hoat dong on dinh
public class FakeUploader : IFileUploader
{
    public async Task<bool> UploadFileAsync(string filePath, IProgress<double> progress, CancellationToken ct)
    {
        for (int i = 0; i <= 100; i += 20)
        {
            await Task.Delay(200, ct);
            progress.Report(i);
        }
        return true;
    }
}