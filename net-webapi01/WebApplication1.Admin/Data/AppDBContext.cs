using MongoDB.Driver;
using CoreLibrary.DbAccess;
using WebApi.Models;
using CoreLibrary.Helpers;

namespace WebApi.Data;

public class AppDBContext
{
    private readonly MongoDbContext _dbContext;

    public AppDBContext(string connectionString, string databaseName)
    {
        _dbContext = new MongoDbContext(AESHelpers.Decrypt(connectionString), databaseName);
    }

    public IMongoCollection<AdminUser> AdminUsers => _dbContext.GetCollection<AdminUser>("Users");
}