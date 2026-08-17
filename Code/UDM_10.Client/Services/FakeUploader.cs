namespace UDM_10.Client.Services;

// TAM THOI - xoa khi co NetworkClient that cua Phu hoat dong on dinh

public class FakeUploader : IFileUploader
{
    private readonly HashSet<string> _usedNames = new();
    private readonly Func<bool> _shouldSimulateError;

    public FakeUploader(Func<bool> shouldSimulateError)
    {
        _shouldSimulateError = shouldSimulateError;
    }

    public async Task<UploadOutcome> UploadFileAsync(string filePath, IProgress<double> progress, CancellationToken ct)
    {
        for (int i = 0; i <= 100; i += 20)
        {
            await Task.Delay(200, ct);
            progress.Report(i);
            if (i == 40 && _shouldSimulateError() && filePath.Contains("loi_test"))
                throw new Exception("Lỗi xử lý file phía Server (giả lập)");
        }

        string name = Path.GetFileName(filePath);
        string finalName = name;
        int counter = 1;
        while (_usedNames.Contains(finalName))
        {
            finalName = $"{Path.GetFileNameWithoutExtension(name)}({counter}){Path.GetExtension(name)}";
            counter++;
        }
        _usedNames.Add(finalName);

        return new UploadOutcome(true, finalName, null);
    }
}
