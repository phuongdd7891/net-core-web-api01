namespace CoreLibrary.DbAccess;

public interface IConnectionThrottlingPipeline
{
    public Task<T> AddRequest<T>(Func<Task<T>> task);
}