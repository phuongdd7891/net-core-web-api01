using CoreLibrary.Repository;
using Gateway.Models;
using Microsoft.Extensions.Options;

namespace Gateway.Services;

public class FileHostedService : IHostedService
{
    private readonly RedisRepository _redisRepository;
    private readonly UploadSettings _uploadSettings;
    public FileHostedService(
        RedisRepository redisRepository,
        IOptions<UploadSettings> uploadSettings
    )
    {
        _uploadSettings = uploadSettings.Value;
        _redisRepository = redisRepository;
        DirectoryInfo dirInfo = new DirectoryInfo(_uploadSettings.UploadDir);
        if (!dirInfo.Exists)
        {
            dirInfo.Create();
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await LoadFileNamesToCacheAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private async Task LoadFileNamesToCacheAsync()
    {
        var files = Directory.EnumerateFiles(_uploadSettings.UploadDir)
                            .Where(a => !string.IsNullOrEmpty(a))
                            .Select(Path.GetFileName)
                            .ToArray();

        if (files.Length > 0)
        {
            var tasks = files.Select(file => _redisRepository.SetAdd(_uploadSettings.CacheName, file!));
            await Task.WhenAll(tasks);
        }
    }
}