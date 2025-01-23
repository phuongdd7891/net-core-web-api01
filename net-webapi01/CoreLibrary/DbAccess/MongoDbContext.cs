using CoreLibrary.Models;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;

namespace CoreLibrary.DbAccess;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;
    private readonly MongoClient _client;

    public MongoDbContext(string connectionString, string databaseName)
    {
        _client = new MongoClient(connectionString);
        _database = _client.GetDatabase(databaseName);
    }

    public MongoDbContext(IConfiguration configuration)
    {
        _client = new MongoClient(configuration.GetConnectionString("MongoDb"));
        _database = _client.GetDatabase(configuration["DatabaseName"]);

    }

    public IMongoCollection<T> GetCollection<T>(string collectionName, string dbName = "") {
        return string.IsNullOrEmpty(dbName) ? _database.GetCollection<T>(collectionName) : _client.GetDatabase(dbName).GetCollection<T>(collectionName);
    }

    public IMongoCollection<RoleAction> RoleActions => GetCollection<RoleAction>("RoleAction");

    public IMongoClient GetClient(string dbName = "") => string.IsNullOrEmpty(dbName) ? _database.Client : _client.GetDatabase(dbName).Client;
}