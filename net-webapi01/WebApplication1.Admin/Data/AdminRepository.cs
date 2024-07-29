using CoreLibrary.Repository;
using Microsoft.AspNetCore.Identity;
using MongoDB.Driver;
using WebApi.Models;
using MongoDB.Driver.Linq;

namespace WebApi.Data;

public class AdminRepository
{
    private readonly IMongoCollection<AdminUser> _users;
    private PasswordHasher<AdminUser> passwordHasher;
    private readonly RedisRepository _redisRepository;

    public AdminRepository(
        AppDBContext _context,
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
        user.CreatedDate = DateTime.Now;
        await _users.InsertOneAsync(user);
    }

    public async Task<List<AdminUser>> ListUsers(bool isCustomer = false)
    {
        return await _users.Find(a => a.IsCustomer == isCustomer || isCustomer == false).ToListAsync();
    }

    public async Task<AdminUser> GetUser(string username) => await _users.Find(a => a.Username == username).FirstOrDefaultAsync();

    public async Task<AdminUser> GetUserById(string id) => await _users.Find(a => a.Id == id).FirstOrDefaultAsync();

    public async Task<bool> VerifyPassword(string username, string password)
    {
        var user = await GetUser(username);
        return user != null && passwordHasher.VerifyHashedPassword(user, user.Password, password) != PasswordVerificationResult.Failed;
    }

    public async Task UpdateUser(AdminUser user, string? password = null)
    {
        var filter = Builders<AdminUser>.Filter.Where(u => u.Username == user.Username);
        user.ModifiedDate = DateTime.Now;
        if (!string.IsNullOrEmpty(password))
        {
            user.Password = passwordHasher.HashPassword(user, password);
        }
        await _users.ReplaceOneAsync(filter, user);
    }
}