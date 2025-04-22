namespace Telega.Application.Services
{
    public interface IFileStorageService : IDisposable
    {
        Task UploadFileAsync(string fileName, Stream fileStream);
        Task<Stream> DownloadFileAsync(string fileName);
        Task RemoveFileAsync(string fileName);
        Task<bool> ObjectExistsAsync(string fileName);
        Task<string> GetPresignedUrlAsync(string filename, int expirySeconds = 3600);
    }
}