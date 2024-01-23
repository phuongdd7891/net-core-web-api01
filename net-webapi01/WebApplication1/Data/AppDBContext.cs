using WebApi.Models;
using IdentityMongo.Models;
using MongoDB.Driver;
using CoreLibrary.DbContext;

public class AppDBContext
{
    private readonly MongoDbContext _dbContext;

    public AppDBContext(string connectionString, string databaseName)
    {
        _dbContext = new MongoDbContext(connectionString, databaseName);
    }

    public MongoDbContext GetContext() => _dbContext;
    public IMongoCollection<Book> Books => _dbContext.GetCollection<Book>("Books");
    public IMongoCollection<BookCategory> BookCategories => _dbContext.GetCollection<BookCategory>("BookCategories");
    public IMongoCollection<UserApiKey> ApiKeys => _dbContext.GetCollection<UserApiKey>("UserApiKeys");
}