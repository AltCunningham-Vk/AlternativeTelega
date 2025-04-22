using Microsoft.Extensions.Configuration;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using Telega.Application.Services;

namespace Telega.Infrastructure.Storage
{
    public class MinioService : IFileStorageService
    {
        private readonly IMinioClient _minioClient;
        private bool _disposed;
        private const string Bucketname = "telegastorage";

        public MinioService(IConfiguration configuration)
        {
            var endpoint = configuration["Minio:Endpoint"];
            var accessKey = configuration["Minio:AccessKey"];
            var secretKey = configuration["Minio:SecretKey"];
            _minioClient = new MinioClient()
                .WithEndpoint(endpoint)
                .WithCredentials(accessKey, secretKey)
                .Build();
        }
        // Загрузка
        public async Task UploadFileAsync(string filename, Stream filestream)
        {
            if (string.IsNullOrEmpty(filename)) throw new ArgumentNullException(nameof(filename));
            if (filestream == null) throw new ArgumentNullException(nameof(filestream));

            var args = new PutObjectArgs()
                .WithBucket(Bucketname)
                .WithObject(filename)
                .WithStreamData(filestream)
                .WithObjectSize(filestream.Length);
            await _minioClient.PutObjectAsync(args).ConfigureAwait(false);
        }
        // Скачивание
        public async Task<Stream> DownloadFileAsync(string filename)
        {
            if (string.IsNullOrEmpty(Bucketname)) throw new ArgumentNullException(nameof(Bucketname));
            if (string.IsNullOrEmpty(filename)) throw new ArgumentNullException(nameof(filename));

            Stream stream = new MemoryStream();
            try
            {
                var args = new GetObjectArgs()
                    .WithObject(filename)
                    .WithBucket(Bucketname)
                    .WithCallbackStream(s => s.CopyTo(stream));

                await _minioClient.GetObjectAsync(args);
                stream.Position = 0;
                return stream;
            }
            catch (ObjectNotFoundException)
            {
                stream.Dispose();
                throw new FileNotFoundException($"Файд {filename} не сушествует в баккете {Bucketname}");
            }
            catch (MinioException ex)
            {
                stream.Dispose();
                throw new FileNotFoundException($"Ошибка загрузки файла {filename}:{Bucketname}", ex);
            }
        }
        // Удаление
        public async Task RemoveFileAsync(string filename)
        {
            try
            {
                var args = new RemoveObjectArgs()
                    .WithObject(filename)
                    .WithBucket(Bucketname);
                await _minioClient.RemoveObjectAsync(args);
            }
            catch (MinioException ex)
            {
                throw new FileNotFoundException($"Ошибка удаление файла {filename}:{Bucketname}", ex);
            }
        }
        // Проверка существования
        public async Task<bool> ObjectExistsAsync(string filename)
        {
            if (string.IsNullOrEmpty(filename)) throw new ArgumentNullException(nameof(filename));
            try
            {
                var args = new StatObjectArgs()
                .WithObject(filename)
                .WithBucket(Bucketname);
                await _minioClient.StatObjectAsync(args);
                return true;
            }
            catch (Minio.Exceptions.MinioException)
            {
                return false;
            }
        }
        // Новый метод для предпросмотра
        public async Task<string> GetPresignedUrlAsync(string filename, int expireSeconds = 3600)
        {
            if (string.IsNullOrEmpty(filename)) throw new ArgumentNullException(nameof(filename));

            var args = new PresignedGetObjectArgs()
                .WithBucket(Bucketname)
                .WithObject(filename)
                .WithExpiry(expireSeconds);
            return await _minioClient.PresignedGetObjectAsync(args);
        }
        // Освобождение
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        // Освобождение
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _minioClient?.Dispose();
            }
            _disposed = true;
        }
        ~MinioService()
        {
            Dispose(false);
        }
    }
}
