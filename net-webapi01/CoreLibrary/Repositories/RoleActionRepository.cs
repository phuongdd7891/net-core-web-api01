using CoreLibrary.DbAccess;
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

    public async Task AddActionsToRole(string roleId, string[] actions, string actor)
    {
        var filter = Builders<RoleAction>.Filter.Where(x => x.RoleId == roleId);
        var roleAction = await _collection.Find(filter).FirstOrDefaultAsync();
        if (roleAction != null)
        {
            roleAction.Actions = actions.ToList();
            roleAction.ModifiedDate = DateTime.UtcNow;
            roleAction.ModifiedBy = actor;
            await _collection.ReplaceOneAsync(filter, roleAction);
        }
        else
        {
            await _collection.InsertOneAsync(new RoleAction
            {
                RoleId = roleId,
                Actions = actions.ToList(),
                ModifiedDate = DateTime.UtcNow,
                ModifiedBy = actor
            });
        }
    }

    public async Task<List<RoleAction>> GetAll()
    {
        return await _collection.Find(_ => true).ToListAsync();
    }

    public async Task DeleteByRoleId(string roleId, IClientSessionHandle? session = null)
    {
        var filter = Builders<RoleAction>.Filter.Where(x => x.RoleId == roleId);
        if (session == null)
        {
            await _collection.DeleteOneAsync(filter);
        }
        else
        {
            await _collection.DeleteOneAsync(session, filter);
        }
    }
}