using MongoDB.Driver;

namespace CoreLibrary.DbAccess;

public class ConnectionThrottlingPipeline: IConnectionThrottlingPipeline
{
    private readonly Semaphore openConnectionSemaphore;

    public ConnectionThrottlingPipeline(IMongoClient client)
    {
        openConnectionSemaphore = new Semaphore( client.Settings.MaxConnectionPoolSize / 2,
            client.Settings.MaxConnectionPoolSize / 2 );
    }

    public async Task<T> AddRequest<T>(Func<Task<T>> task)
    {
        openConnectionSemaphore.WaitOne();
        try
        {
            var result = await task();
            return result;
        }
        finally
        {
            openConnectionSemaphore.Release();
        }
    }
}