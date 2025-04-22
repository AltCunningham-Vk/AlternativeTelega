using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Telega.Infrastructure.Caching
{
    public interface IRedisCacheService
    {
        Task<T?> GetTAsync<T>(string key);
        Task SetTAsync<T>(string key, T value, TimeSpan? expiry = null);
        Task RemoveAsync(string key);
    }
}
