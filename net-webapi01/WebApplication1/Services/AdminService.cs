using Microsoft.AspNetCore.Identity;
using MongoDB.Driver;
using WebApi.Models.Admin;
using WebApi.Models.Requests;

namespace WebApi.Services;

public class AdminService
{
    private readonly IMongoCollection<AdminUser> _users;
    private PasswordHasher<AdminUser> passwordHasher;

    public AdminService(
        AppAdminDBContext _context
    )
    {
        _users = _context.AdminUsers;
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
}