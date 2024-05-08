using CoreLibrary.Repository;
using WebApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using WebApi.Hubs;
using WebApi.Services;
using WebApi.Models.Admin;
using System.Security.Claims;
using Newtonsoft.Json;

namespace WebApi.Controllers;

public class BaseController : ControllerBase
{
    private IHubContext<UserNotifications>? _hubContext;
    protected IHubContext<UserNotifications> hubContext => _hubContext ?? (_hubContext = HttpContext.RequestServices.GetRequiredService<IHubContext<UserNotifications>>());
    private UserManager<ApplicationUser>? _userManager;
    protected UserManager<ApplicationUser> userManager => _userManager ?? (_userManager = HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>());

    [HttpPost("notify")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task NotifyUser([FromBody] string message, string username = "")
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

    public AdminProfile? Profile
    {
        get
        {
            var userData = User.FindFirstValue(ClaimTypes.UserData);
            if (!string.IsNullOrEmpty(userData))
            {
                var profile = JsonConvert.DeserializeObject<AdminProfile>(userData);
                return profile;
            }
            return null;
        }
    }
}