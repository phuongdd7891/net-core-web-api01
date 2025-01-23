using MongoDB.Driver;
using CoreLibrary.DbAccess;
using AdminMicroService.Models;
using CoreLibrary.DataModels;

namespace AdminMicroService.Data;

public class AppDBContext
{
    private readonly MongoDbContext _dbContext;
    private readonly string adminDbName;

    public AppDBContext(
        MongoDbContext mongoDbContext,
        IConfiguration configuration
    )
    {
        _dbContext = mongoDbContext;
        adminDbName = configuration.GetValue<string>("AdminDatabaseName") ?? string.Empty;
        if (string.IsNullOrEmpty(adminDbName))
        {
            throw new ArgumentNullException("AdminDatabaseName is not found in appsettings.json");
        }
    }

    public IMongoCollection<AdminUser> AdminUsers => _dbContext.GetCollection<AdminUser>("Users", adminDbName);
    public IMongoCollection<ApplicationUser> AppUsers => _dbContext.GetCollection<ApplicationUser>("Users");
    public IMongoCollection<ApplicationRole> AppRoles => _dbContext.GetCollection<ApplicationRole>("Roles");

    public IMongoClient GetClient()
    {
        return _dbContext.GetClient();
    }

    public IMongoClient GetAdminClient()
    {
        return _dbContext.GetClient(adminDbName);
    }
}