using AdminWeb.Models;
using AdminWeb.Models.Response;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Security.Claims;

namespace AdminWeb.Controllers
{
    public class BaseController : Controller
    {
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
}
