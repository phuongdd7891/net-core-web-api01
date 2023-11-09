using StackExchange.Redis.Extensions.Core.Abstractions;

namespace CoreLibrary.Repository;

public class RedisRepository
{
    private readonly IRedisClient _redisClient;

    public RedisRepository(
        IRedisClient redisClient)
    {
        _redisClient = redisClient;
    }

    public async Task Set(string key, string value, int expiryMins)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentException("The key should not be null or empty.");
        }

        await _redisClient.Db0.AddAsync(key, value, TimeSpan.FromMinutes(expiryMins));
    }

    public async Task<string> Get(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentException("The key should not be null or empty.");
        }
        var value = await _redisClient.Db0.GetAsync<string>(key);
        return value ?? string.Empty;
    }

    public async Task<bool> HasKey(string key)
    {
        return await _redisClient.Db0.ExistsAsync(key);
    }

    public async Task UpdateExpireKey(string key, int expiryMins)
    {
        await _redisClient.Db0.UpdateExpiryAsync(key, TimeSpan.FromMinutes(expiryMins));
    }

    public async Task SetEntity<T>(string key, T entity, int expiryMins)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentException("The key should not be null or empty.");
        }
        await _redisClient.Db0.AddAsync(key, entity, TimeSpan.FromMinutes(expiryMins));
    }

    public async Task<T?> GetEntity<T>(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentException("The key should not be null or empty.");
        }
        return await _redisClient.Db0.GetAsync<T>(key);
    }

    public async Task SetHashEntity<T>(string key, Dictionary<string, T> entities)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentException("The key should not be null or empty.");
        }
        await _redisClient.Db0.HashSetAsync(key, entities);
    }

    public async Task<Dictionary<string, T>> GetHashEntity<T>(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentException("The key should not be null or empty.");
        }
        return (Dictionary<string, T>)await _redisClient.Db0.HashGetAllAsync<T>(key);
    }

    public async Task<T?> GetHashByField<T>(string key, string field)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentException("The key should not be null or empty.");
        }
        var value = await _redisClient.Db0.HashGetAsync<T>(key, field);
        return value;
    }
}