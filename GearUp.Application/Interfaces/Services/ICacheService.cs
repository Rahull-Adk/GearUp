namespace GearUp.Application.Interfaces.Services
{
    public interface ICacheService
    {
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
        Task<T?> GetAsync<T>(string key);
        Task RemoveAsync(string key);

        Task SetHashAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class;
        Task<T?> GetHashAsync<T>(string key) where T : class;
        Task UpdateHashFieldAsync<TField>(string key, string field, TField value);
        Task RemoveHashAsync(string key);
    }
}
