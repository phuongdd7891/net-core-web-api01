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

    public async Task Add(string[] actions, string[] roles)
    {
        var filter = Builders<RoleAction>.Filter.Where(x => actions.Contains(x.RequestAction));
        var roleActions = await _collection.Find(filter).ToListAsync();
        var roleList = roles.ToList();
        if (roleActions == null)
        {
            foreach (var action in actions)
            {
                await _collection.InsertOneAsync(new RoleAction
                {
                    RequestAction = action,
                    Roles = roleList
                });
            }
        }
        else
        {
            var builder = Builders<RoleAction>.Update;
            var reqActions = new List<string>(roleActions.Count);
            foreach (var item in roleActions)
            {
                reqActions.Add(item.RequestAction);
                var update = builder.Set("Roles", roleList);
                await _collection.UpdateManyAsync(filter, update);
            }
            var newActions = actions.Where(x => !reqActions.Contains(x)).ToList();
            newActions.ForEach(async x => await _collection.InsertOneAsync(new RoleAction
            {
                RequestAction = x,
                Roles = roleList
            }));
        }
    }

    public async Task<List<RoleAction>> GetAll()
    {
        return await _collection.Find(_ => true).ToListAsync();
    }

    public async Task<DeleteResult> Delete(string action)
    {
        var filter = Builders<RoleAction>.Filter.Where(x => x.RequestAction == action);
        return await _collection.DeleteOneAsync(filter);
    }
}