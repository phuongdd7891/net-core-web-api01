
using CoreLibrary.Repository;
using DnsClient.Protocol;
using WebApi.Models;
using Microsoft.AspNetCore.Identity;
using NLog;
using NLog.Web;

namespace WebApi.Services;

public class InitializeCacheService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Logger logger = LogManager.Setup()
                       .LoadConfigurationFromAppSettings()
                       .GetCurrentClassLogger();

    public InitializeCacheService(
        IServiceProvider serviceProvider
    )
    {
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            var cacheService = scope.ServiceProvider.GetService<CacheService>()!;
            await cacheService.LoadUserRoles(true);
            await cacheService.LoadRoleActions(true);
            logger.Info("caches load done");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

public class CacheService
{
    private RedisRepository _redisRepository;
    private RoleManager<ApplicationRole> _roleManager;
    private RoleActionRepository _roleActionRepository;

    public CacheService(
        RedisRepository redisRepository,
        RoleActionRepository roleActionRepository,
        IServiceProvider serviceProvider
    )
    {
        _redisRepository = redisRepository;
        _roleActionRepository = roleActionRepository;
        _roleManager = serviceProvider.CreateScope().ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
    }

    public async Task LoadUserRoles(bool isReload = false)
    {
        if (isReload)
        {
            await RemoveKey(Const.USER_ROLES_KEY);
        }
        var roles = _roleManager.Roles.ToDictionary(a => a.Id.ToString(), a => a.Name);
        await _redisRepository.SetHashEntity<string>(Const.USER_ROLES_KEY, roles!);
    }

    public async Task LoadRoleActions(bool isReload = false)
    {
        if (isReload)
        {
            await RemoveKey(Const.ROLE_ACTION_KEY);
        }
        var actions = await _roleActionRepository.GetAll();
        if (actions.Any(x => !string.IsNullOrEmpty(x.RoleId)))
        {
            await _redisRepository.SetHashEntity(Const.ROLE_ACTION_KEY, actions.Where(x => !string.IsNullOrEmpty(x.RoleId)).ToDictionary(a => a.RoleId!, a => a.Actions));
        }
    }

    private async Task RemoveKey(string key)
    {
        if (await _redisRepository.HasKey(key))
        {
            await _redisRepository.Remove(key);
        }
    }
}