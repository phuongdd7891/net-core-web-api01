using WebApi.Models;
using MongoDB.Driver;
using CoreLibrary.DbAccess;

public class AppDBContext
{
    private readonly MongoDbContext _dbContext;

    public AppDBContext(string connectionString, string databaseName)
    {
        _dbContext = new MongoDbContext(connectionString, databaseName);
    }

    public IMongoCollection<Book> Books => _dbContext.GetCollection<Book>("Books");
    public IMongoCollection<BookCategory> BookCategories => _dbContext.GetCollection<BookCategory>("BookCategories");
    public IMongoCollection<UserApiKey> ApiKeys => _dbContext.GetCollection<UserApiKey>("UserApiKeys");
    public IMongoCollection<Cart> Carts => _dbContext.GetCollection<Cart>("Carts");
}