using CoreLibrary.DbContext;
using CoreLibrary.Models;
using MongoDB.Driver;

public class RoleActionRepository
{
    private readonly IMongoCollection<RoleAction> _collection;

    public RoleActionRepository(
        MongoDbContext context
    )
    {
        _collection = context.RoleActions;
    }

    public async Task Add(string action, List<string> roles)
    {
        await _collection.InsertOneAsync(new RoleAction
        {
            RequestAction = action,
            Roles = roles
        });
    }

    public async Task<List<RoleAction>> GetAll()
    {
        return await _collection.Find(_ => true).ToListAsync();
    }
}