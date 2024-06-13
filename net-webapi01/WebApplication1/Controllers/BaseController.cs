using CoreLibrary.Repository;
using WebApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApi.Models.Admin;
using System.Security.Claims;
using Newtonsoft.Json;

namespace WebApi.Controllers;

public class BaseController : ControllerBase
{
    private UserManager<ApplicationUser>? _userManager;
    protected UserManager<ApplicationUser> userManager => _userManager ?? (_userManager = HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>());

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