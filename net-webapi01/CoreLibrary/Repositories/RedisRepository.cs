using StackExchange.Redis.Extensions.Core.Abstractions;

namespace CoreLibrary.Repository;

public class RedisRepository
{
    private readonly IRedisClient _redisClient;

    private readonly string _emptyKeyMessage = "The key should not be null or empty.";

    public RedisRepository(
        IRedisClient redisClient)
    {
        _redisClient = redisClient;
    }

    public async Task Add(string key, string value, TimeSpan expiry)
    {
        ErrorStatuses.ThrowInternalErr(_emptyKeyMessage, string.IsNullOrEmpty(key));
        await _redisClient.Db0.AddAsync(key, value, expiry);
    }

    public async Task<string> Get(string key)
    {
        ErrorStatuses.ThrowInternalErr(_emptyKeyMessage, string.IsNullOrEmpty(key));
        var value = await _redisClient.Db0.GetAsync<string>(key);
        return value ?? string.Empty;
    }

    public async Task Remove(string key)
    {
        ErrorStatuses.ThrowInternalErr(_emptyKeyMessage, string.IsNullOrEmpty(key));
        await _redisClient.Db0.RemoveAsync(key);
    }

    public async Task<bool> HasKey(string key)
    {
        return await _redisClient.Db0.ExistsAsync(key);
    }

    public async Task SetAdd(string key, string value)
    {
        await _redisClient.Db0.SetAddAsync(key, value);
    }

    public async Task<bool> SetContains(string key, string value)
    {
        return await _redisClient.Db0.SetContainsAsync(key, value);
    }

    public async Task<T[]> SetMembers<T>(string key)
    {
        return await _redisClient.Db0.SetMembersAsync<T>(key);
    }

    public async Task UpdateExpireKey(string key, TimeSpan expiry)
    {
        await _redisClient.Db0.UpdateExpiryAsync(key, expiry);
    }

    public async Task SetEntity<T>(string key, T entity, TimeSpan expiry)
    {
        ErrorStatuses.ThrowInternalErr(_emptyKeyMessage, string.IsNullOrEmpty(key));
        await _redisClient.Db0.AddAsync(key, entity, expiry);
    }

    public async Task<T?> GetEntity<T>(string key)
    {
        ErrorStatuses.ThrowInternalErr(_emptyKeyMessage, string.IsNullOrEmpty(key));
        return await _redisClient.Db0.GetAsync<T>(key);
    }

    public async Task ReplaceEntity<T>(string key, T entity, TimeSpan expiry)
    {
        ErrorStatuses.ThrowInternalErr(_emptyKeyMessage, string.IsNullOrEmpty(key));
        if (await HasKey(key))
        {
            await _redisClient.Db0.ReplaceAsync(key, entity, expiry);
        }
    }

    public async Task SetHashEntity<T>(string key, Dictionary<string, T> entities)
    {
        ErrorStatuses.ThrowInternalErr(_emptyKeyMessage, string.IsNullOrEmpty(key));
        await _redisClient.Db0.HashSetAsync(key, entities);
    }

    public async Task<Dictionary<string, T>> GetHashEntity<T>(string key)
    {
        ErrorStatuses.ThrowInternalErr(_emptyKeyMessage, string.IsNullOrEmpty(key));
        return (Dictionary<string, T>)await _redisClient.Db0.HashGetAllAsync<T>(key);
    }

    public async Task<T?> GetHashByField<T>(string key, string field)
    {
        ErrorStatuses.ThrowInternalErr(_emptyKeyMessage, string.IsNullOrEmpty(key));
        var value = await _redisClient.Db0.HashGetAsync<T>(key, field);
        return value;
    }

    public async Task DeleteHashByField(string key, string field)
    {
        ErrorStatuses.ThrowInternalErr(_emptyKeyMessage, string.IsNullOrEmpty(key));
        await _redisClient.Db0.HashDeleteAsync(key, field);
    }

    public async Task<List<T?>> GetHashValues<T>(string key)
    {
        ErrorStatuses.ThrowInternalErr(_emptyKeyMessage, string.IsNullOrEmpty(key));
        var value = await _redisClient.Db0.HashValuesAsync<T>(key);
        return value.ToList();
    }

    public async Task CloseConnection()
    {
        await _redisClient.ConnectionPoolManager.GetConnection().CloseAsync();
    }
}