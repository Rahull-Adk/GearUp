using GearUp.Application.Interfaces.Services;
using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using System.Reflection;
using System.Text.Json;

namespace GearUp.Application.Services
{
    public class CacheService : ICacheService
    {
        private readonly IDistributedCache _cache;
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _db;

        public CacheService(IDistributedCache cache, IConnectionMultiplexer redis)
        {
            _cache = cache;
            _redis = redis;
            _db = _redis.GetDatabase();
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

        public async Task SetHashAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
        {
            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var hashEntries = properties
                .Select(p => new HashEntry(p.Name, JsonSerializer.Serialize(p.GetValue(value))))
                .ToArray();

            await _db.HashSetAsync(key, hashEntries);
            if (expiration.HasValue)
            {
                await _db.KeyExpireAsync(key, expiration);
            }
        }

        public async Task<T?> GetHashAsync<T>(string key) where T : class
        {
            var hashEntries = await _db.HashGetAllAsync(key);
            if (hashEntries.Length == 0) return default;

            var obj = Activator.CreateInstance<T>();
            foreach (var entry in hashEntries)
            {
                var prop = typeof(T).GetProperty(entry.Name.ToString());
                if (prop != null && prop.CanWrite)
                {
                    try
                    {
                        var val = JsonSerializer.Deserialize(entry.Value.ToString(), prop.PropertyType);
                        prop.SetValue(obj, val);
                    }
                    catch
                    {
                        // Skip if deserialization fails for a specific field
                    }
                }
            }
            return obj;
        }

        public async Task UpdateHashFieldAsync<TField>(string key, string field, TField value)
        {
            await _db.HashSetAsync(key, field, JsonSerializer.Serialize(value));
        }

        public async Task RemoveHashAsync(string key)
        {
            await _db.KeyDeleteAsync(key);
        }
    }
}
