using CoreLibrary.Models;
using MongoDB.Driver;

namespace CoreLibrary.DbContext;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;
    private readonly MongoClient _client;

    public MongoDbContext(string connectionString, string databaseName)
    {
        _client = new MongoClient(connectionString);
        _database = _client.GetDatabase(databaseName);
    }

    public IMongoCollection<T> GetCollection<T>(string collectionName) {
        return _database.GetCollection<T>(collectionName);
    }

    public IMongoCollection<RoleAction> RoleActions => GetCollection<RoleAction>("RoleAction");
}