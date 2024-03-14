using CoreLibrary.Repository;
using Microsoft.AspNetCore.Identity;
using MongoDB.Driver;
using NLog.LayoutRenderers;
using WebApi.Models.Admin;
using WebApi.Models.Requests;

namespace WebApi.Services;

public class AdminService
{
    private readonly IMongoCollection<AdminUser> _users;
    private PasswordHasher<AdminUser> passwordHasher;
    private readonly RedisRepository _redisRepository;

    public AdminService(
        AppAdminDBContext _context,
        RedisRepository redisRepository
    )
    {
        _users = _context.AdminUsers;
        _redisRepository = redisRepository;
        passwordHasher = new PasswordHasher<AdminUser>();
    }

    public async Task CreateUser(AdminUser user)
    {
        var hashedPwd = passwordHasher.HashPassword(user, user.Password);
        user.Password = hashedPwd;
        await _users.InsertOneAsync(user);
    }

    public async Task<AdminUser> GetUser(string username) => await _users.Find(a => a.Username == username).FirstOrDefaultAsync();

    public async Task<bool> VerifyPassword(string username, string password)
    {
        var user = await GetUser(username);
        return user != null && passwordHasher.VerifyHashedPassword(user, user.Password, password) != PasswordVerificationResult.Failed;
    }

    public async Task UpdateUser(AdminUser user)
    {
        var filter = Builders<AdminUser>.Filter.Where(u => u.Username == user.Username);
        await _users.ReplaceOneAsync(filter, user);
    }
}