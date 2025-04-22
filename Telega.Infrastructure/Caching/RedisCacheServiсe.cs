using StackExchange.Redis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Telega.Application.Services;

namespace Telega.Infrastructure.Caching
{
    public class RedisCacheService : ICacheService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _db;
        private readonly JsonSerializerOptions _jsonOptions;

        public RedisCacheService(IConnectionMultiplexer redis)
        {
            _redis = redis ?? throw new ArgumentNullException(nameof(redis));
            _db = _redis.GetDatabase();
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReferenceHandler = ReferenceHandler.Preserve
            };
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            try
            {
                var value = await _db.StringGetAsync(key);
                return value.HasValue
                    ? JsonSerializer.Deserialize<T>(value, _jsonOptions)
                    : default;
            }
            catch (RedisException ex)
            {
                throw new InvalidOperationException($"Ошибка при извлечении ключа кэша '{key}': {ex.Message}", ex);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Ошибка десериализации значения кэша для ключа '{key}': {ex.Message}", ex);
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));
            if (value == null)
                throw new ArgumentNullException(nameof(value), "Не удается кэшировать нулевое значение.");

            try
            {
                var serializedValue = JsonSerializer.Serialize(value, _jsonOptions);
                await _db.StringSetAsync(key, serializedValue, expiry);
            }
            catch (RedisException ex)
            {
                throw new InvalidOperationException($"Ошибка при установке ключа кэша '{key}': {ex.Message}", ex);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Ошибка при сериализации значения для ключа '{key}': {ex.Message}", ex);
            }
        }

        public async Task RemoveAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            try
            {
                await _db.KeyDeleteAsync(key);
            }
            catch (RedisException ex)
            {
                throw new InvalidOperationException($"Ошибка при удалении ключа кэша '{key}': {ex.Message}", ex);
            }
        }
    }
}
