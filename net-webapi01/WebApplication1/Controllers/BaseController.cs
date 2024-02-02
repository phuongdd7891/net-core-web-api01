using CoreLibrary.Repository;
using IdentityMongo.Models;
using Microsoft.AspNetCore.Identity;
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
    private UserManager<ApplicationUser>? _userManager;
    protected UserManager<ApplicationUser> userManager => _userManager ?? (_userManager = HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>());

    [HttpPost("notify")]
    public async Task NotifyUser([FromBody]string message, string username = "")
    {
        if (string.IsNullOrEmpty(username))
        {
            await hubContext.Clients.All.SendAsync("Notify", message);
        }
        else
        {
            var user = await userManager.FindByNameAsync(username);
            if (user != null)
            {
                await hubContext.Clients.User(user.Id.ToString()).SendAsync("Notify", $"{message}");
            }
        }
    }
}