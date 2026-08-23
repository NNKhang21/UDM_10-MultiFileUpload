using System;
using System.Threading;
using System.Threading.Tasks;

namespace UDM_10.Client.Services
{
    public class UploadQueue
    {
        private readonly IFileUploader _uploader;
        private readonly SemaphoreSlim _semaphore;

        public UploadQueue(IFileUploader uploader, int maxConcurrentUploads = 2)
        {
            _uploader = uploader ?? throw new ArgumentNullException(nameof(uploader));
            _semaphore = new SemaphoreSlim(maxConcurrentUploads, maxConcurrentUploads);
        }

        public async Task<UploadOutcome> EnqueueAsync(string filePath, IProgress<double> progress, CancellationToken ct)
        {
            await _semaphore.WaitAsync(ct);
            try
            {
                return await _uploader.UploadFileAsync(filePath, progress, ct);
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}