using System.Text.Json;
using CoreLibrary.Repository;
using WebApi.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace WebApi.Services;

public class ApiKeyService
{
    private readonly IMongoCollection<UserApiKey> _apiKeyCollection;
    private readonly RedisRepository _redisRepository;
    private readonly TimeSpan defaultExpiryMins = TimeSpan.FromMinutes(24 * 60);

    public ApiKeyService(
        AppDBContext _context,
        RedisRepository redisRepository
    )
    {
        _apiKeyCollection = _context.ApiKeys;
        _redisRepository = redisRepository;
    }

    public async Task<UserApiKey> CreateMongoToken(ApplicationUser user)
    {
        var existedKey = await _apiKeyCollection.Find(a => a.User!.Id == user.Id).SingleOrDefaultAsync();
        if (existedKey != null)
        {
            existedKey.LoginTime = DateTime.Now;
            var filter = Builders<UserApiKey>.Filter
                .Eq(a => a.Id, existedKey.Id);
            var update = Builders<UserApiKey>.Update
                .Set(a => a.LoginTime, existedKey.LoginTime);
            await _apiKeyCollection.UpdateOneAsync(filter, update);
            return existedKey;
        }
        var newApiKey = new UserApiKey
        {
            User = user,
            Username = user.UserName!,
            Value = GenerateApiKeyValue(),
            LoginTime = DateTime.Now
        };
        await _apiKeyCollection.InsertOneAsync(newApiKey);
        return newApiKey;
    }

    public async Task<UserApiKey> CreateRedisToken(ApplicationUser user)
    {
        var redisKey = user.UserName!;
        var userKey = new UserApiKey
        {
            User = user,
            Username = redisKey,
            LoginTime = DateTime.Now
        };
        var hasKey = await _redisRepository.HasKey(redisKey);
        if (hasKey)
        {
            var token = await _redisRepository.Get(redisKey);
            userKey.Value = token;
            await _redisRepository.UpdateExpireKey(redisKey, defaultExpiryMins);
            await _redisRepository.UpdateExpireKey($"{redisKey}:{token}", defaultExpiryMins);
        }
        else
        {
            var token = GenerateApiKeyValue();
            userKey.Value = token;
            await _redisRepository.Add(redisKey, token, defaultExpiryMins);
            await _redisRepository.SetEntity<ApplicationUser>($"{redisKey}:{token}", user, defaultExpiryMins);
        }
        return userKey;
    }

    public async Task RemoveRedisToken(string token)
    {
        var hasKey = await _redisRepository.HasKey(token);
        if (hasKey)
        {
            await _redisRepository.Remove(token);
        }
    }

    public async Task<ApplicationUser?> GetRequestUser(HttpRequest request)
    {
        var user = request.Query["u"];
        var token = request.Headers["ApiKey"];
        var key = $"{user}:{token}";
        if (await _redisRepository.HasKey(key))
        {
            return await _redisRepository.GetEntity<ApplicationUser>(key)!;
        }
        return null;
    }

    private string GenerateApiKeyValue() =>
        $"{Guid.NewGuid()}-{DateTime.UtcNow.Ticks}";
}