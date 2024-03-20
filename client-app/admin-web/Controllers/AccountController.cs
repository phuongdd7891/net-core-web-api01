using AdminWeb.Models;
using AdminWeb.Services;
using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Security.Claims;

namespace AdminWeb.Controllers
{
    public class AccountController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly INotyfService _notyfService;
        private readonly OperationService _opService;
        
        public AccountController(ILogger<HomeController> logger, OperationService opService, INotyfService notyfService)
        {
            _logger = logger;
            _notyfService = notyfService;
            _opService = opService;
        }

        public IActionResult Index() { return View(); }

        public IActionResult Login()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Login(LoginModel loginModel)
        {
            try
            {
                var result = await _opService.Login(loginModel.UserName, loginModel.Password);
                var authCookie = new AuthCookie
                {
                    Token = result.Data!.Token,
                    Username = result.Data.Username,
                };
                HttpContext.Response.Cookies.Append(Const.AuthenticationKey, JsonConvert.SerializeObject(authCookie));
                _notyfService.RemoveAll();
            }
            catch (Exception ex)
            {
                _notyfService.Error(ex.InnerException?.Message ?? ex.Message);
                return View("Login", loginModel);
            }
            return RedirectToAction("Index", "Users");
        }

        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _opService.Logout();
            return RedirectToAction("Login");
        }

        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var profile = await _opService.GetProfile();
            var authCookie = new AuthCookie
            {
                Username = User.Identity!.Name!,
                Token = User.Claims.FirstOrDefault(a => a.Type == "Token")!.Value,
                Profile = profile.Data
            };
            authCookie.Profile = profile.Data;
            HttpContext.Response.Cookies.Append(Const.AuthenticationKey, JsonConvert.SerializeObject(authCookie));
            return Json(profile.Data);
        }
    }
}
