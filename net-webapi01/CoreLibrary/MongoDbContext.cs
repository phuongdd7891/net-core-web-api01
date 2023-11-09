using CoreLibrary.Models;
using MongoDB.Driver;

namespace CoreLibrary.DbContext;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(string connectionString, string databaseName)
    {
        var client = new MongoClient(connectionString);
        _database = client.GetDatabase(databaseName);
    }

    public IMongoCollection<T> GetCollection<T>(string collectionName) {
        return _database.GetCollection<T>(collectionName);
    }

    public IMongoCollection<RoleAction> RoleActions => GetCollection<RoleAction>("RoleAction");
}