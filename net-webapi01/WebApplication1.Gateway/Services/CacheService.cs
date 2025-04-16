using Adminauthservice;
using CoreLibrary.Const;
using CoreLibrary.Helpers;
using CoreLibrary.Repository;
using Grpc.Core;
using Userservice;
using ILogger = Serilog.ILogger;

namespace Gateway.Services;

public class InitializeCacheService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;

    public InitializeCacheService(
        IServiceProvider serviceProvider,
        ILogger logger
    )
    {
        _serviceProvider = serviceProvider;
        _logger = logger.ForContext<InitializeCacheService>();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            var cacheService = scope.ServiceProvider.GetService<CacheService>();
            if (cacheService == null)
            {
                _logger.Error("CacheService is null");
                return;
            }
            await cacheService.LoadUserRoles(true);
            await cacheService.LoadRoleActions(true);
            _logger.Information("caches load done");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

public class CacheService
{
    private readonly ILogger _logger;
    private readonly TimeSpan expiryMinutes = TimeSpan.FromMinutes(24 * 60);
    private RedisRepository _redisRepository;
    private readonly IServiceProvider _serviceProvider;

    public CacheService(
        ILogger logger,
        RedisRepository redisRepository,
        IServiceProvider serviceProvider
    )
    {
        _logger = logger.ForContext<CacheService>();
        _redisRepository = redisRepository;
        _serviceProvider = serviceProvider;
    }

    private Metadata DefaultHeader => new Metadata()
    {
        { "SpecialAuthorization", AESHelpers.Encrypt($"*:{DateTime.UtcNow.AddMinutes(5):yyyy-MM-dd HH:mm}")},
        { "Username", "*" }
    };

    public async Task LoadUserRoles(bool isReload = false)
    {
        if (isReload)
        {
            await RemoveKey(Const.USER_ROLES_KEY);
        }
        using (var scope = _serviceProvider.CreateScope())
        {
            var userServiceClient = scope.ServiceProvider.GetRequiredService<UserServiceProto.UserServiceProtoClient>();
            var rolesReply = await userServiceClient.GetUserRolesAsync(new GetUserRolesRequest(), DefaultHeader);
            var roles = rolesReply.Data.ToDictionary(a => a.Id.ToString(), a => a.Name);
            await _redisRepository.SetHashEntity<string>(Const.USER_ROLES_KEY, roles!);
            _logger.Information("Loaded user roles from gRPC service: {@roles}", rolesReply.Data.Count);
        }
    }

    public async Task LoadRoleActions(bool isReload = false)
    {
        if (isReload)
        {
            await RemoveKey(Const.ROLE_ACTION_KEY);
        }
        using (var scope = _serviceProvider.CreateScope())
        {
            var userServiceClient = scope.ServiceProvider.GetRequiredService<UserServiceProto.UserServiceProtoClient>();
            var actionsReply = await userServiceClient.GetRoleActionsAsync(new Google.Protobuf.WellKnownTypes.Empty(), DefaultHeader);
            var actions = actionsReply.Data.ToList();
            if (actions.Any(x => !string.IsNullOrEmpty(x.RoleId)))
            {
                await _redisRepository.SetHashEntity(Const.ROLE_ACTION_KEY, actions.Where(x => !string.IsNullOrEmpty(x.RoleId)).ToDictionary(a => a.RoleId!, a => a.Actions));
            }
            _logger.Information("Loaded role actions from gRPC service: {@actions}", actionsReply.Data.Count);
        }
    }

    public async Task UpdateUser(UserViewModel userVM)
    {
        if (await _redisRepository.HasKey(userVM.UserName!))
        {
            await _redisRepository.ReplaceEntity(userVM.UserName!, userVM, expiryMinutes);
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