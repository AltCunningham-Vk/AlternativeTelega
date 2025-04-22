using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Telega.Infrastructure.Storage
{
    public interface IMinioService : IDisposable
    {
        Task UploadFileAsync(string filename, Stream filestream);
        Task<Stream> DownloadFileAsync(string filename);
        Task RemoveFileAsync(string filename);
        Task<bool> ObjectExistsAsync(string filename);
    }
}
