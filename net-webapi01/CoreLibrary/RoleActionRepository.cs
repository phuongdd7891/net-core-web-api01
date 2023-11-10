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

    public async Task Add(string action, string role)
    {
        var filter = Builders<RoleAction>.Filter.Where(x => x.RequestAction == action);
        var roleAction = await _collection.Find(filter).FirstOrDefaultAsync();
        if (roleAction == null)
        {
            await _collection.InsertOneAsync(new RoleAction
            {
                RequestAction = action,
                Roles = new List<string> { role }
            });
        }
        else
        {
            var builder = Builders<RoleAction>.Update;
            var roles = roleAction.Roles;
            if (!roles.Contains(role))
            {
                roles.Add(role);
            }
            var update = builder.Set("Roles", roles);
            await _collection.UpdateOneAsync(filter, update);
        }
    }

    public async Task<List<RoleAction>> GetAll()
    {
        return await _collection.Find(_ => true).ToListAsync();
    }
}