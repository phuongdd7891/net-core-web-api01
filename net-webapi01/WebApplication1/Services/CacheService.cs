
using CoreLibrary.Repository;
using DnsClient.Protocol;
using IdentityMongo.Models;
using Microsoft.AspNetCore.Identity;

namespace WebApi.Services;

public class InitializeCacheService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;

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
            await cacheService.LoadUserRoles();
            await cacheService.LoadRoleActions();
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

    public async Task LoadUserRoles()
    {
        var roles = _roleManager.Roles.ToDictionary(a => a.Id.ToString(), a => a.Name);
        await _redisRepository.SetHashEntity<string>(Const.userRolesKey, roles!);
    }

    public async Task LoadRoleActions()
    {
        var actions = await _roleActionRepository.GetAll();
        await _redisRepository.SetHashEntity<List<string>>(Const.roleActionKey, actions.ToDictionary(a => a.RequestAction, a => a.Roles));
    }
}