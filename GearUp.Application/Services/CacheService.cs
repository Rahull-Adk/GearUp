using GearUp.Application.Interfaces.Services;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace GearUp.Application.Services
{
    public class CacheService : ICacheService
    {
        private readonly IDistributedCache _cache;
        public CacheService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            var data = await _cache.GetStringAsync(key);
            if (string.IsNullOrEmpty(data)) return default;

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            return JsonSerializer.Deserialize<T>(data, options);
        }

        public async Task RemoveAsync(string key)
        {
            await _cache.RemoveAsync(key);
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            var opt = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(15)
            };
            await _cache.SetStringAsync(key, JsonSerializer.Serialize(value), opt);
        }
    }
}
