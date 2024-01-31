using CoreLibrary.Repository;
using IdentityMongo.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using WebApi.Hubs;
using WebApi.Services;

namespace WebApi.Controllers;

public class BaseController: ControllerBase
{
    private IHubContext<UserNotifications>? _hubContext;
    protected IHubContext<UserNotifications> hubContext => _hubContext ?? (_hubContext = HttpContext.RequestServices.GetRequiredService<IHubContext<UserNotifications>>());
    private ApiKeyService? _apiKeyService;
    protected ApiKeyService apiKeyService => _apiKeyService ?? (_apiKeyService = HttpContext.RequestServices.GetService<ApiKeyService>())!;
    private RedisRepository? _redisRepository;
    protected RedisRepository redisRepository => _redisRepository ?? (_redisRepository = HttpContext.RequestServices.GetRequiredService<RedisRepository>());

    [NonAction]
    public async Task<ApplicationUser?> GetRequestUser()
    {
        return await apiKeyService.GetRequestUser(Request);
    }

    [NonAction]
    public async Task NotifyUser(string message, string username = "")
    {
        if (string.IsNullOrEmpty(username))
        {
            await hubContext.Clients.All.SendAsync("Notify", message);
        }
        else
        {
            var connId = await redisRepository.Get(username);
            await hubContext.Clients.Client(connId).SendAsync("Notify", $"{connId} => {message}");
        }
    }
}