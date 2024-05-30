using MongoDB.Driver;
using CoreLibrary.DbAccess;
using WebApi.Models.Admin;
using CoreLibrary.Helpers;

public class AppAdminDBContext
{
    private readonly MongoDbContext _dbContext;

    public AppAdminDBContext(string connectionString, string databaseName)
    {
        _dbContext = new MongoDbContext(AESHelpers.Decrypt(connectionString), databaseName);
    }

    public IMongoCollection<AdminUser> AdminUsers => _dbContext.GetCollection<AdminUser>("Users");
}