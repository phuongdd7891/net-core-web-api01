using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using AdminWeb.Models;
using AdminWeb.Services;
using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.Extensions.Primitives;
using Microsoft.AspNetCore.Authorization;
namespace AdminWeb.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly INotyfService _notyfService;
    private readonly ToastMessageService _toastMsgService;
    private readonly AuthService _authService;

    public HomeController(
        ILogger<HomeController> logger,
        INotyfService notyfService,
        ToastMessageService toastMsgService,
        AuthService authService)
    {
        _logger = logger;
        _notyfService = notyfService;
        _toastMsgService = toastMsgService;
        _authService = authService;
    }
    public IActionResult Index()
    {

        return View();
    }
    [HttpPost]
    [Route("changePassword")]
    public async Task<IActionResult> ChangePassword(IFormCollection formCol)
    {
        var currentPwd = formCol["CurrentPassword"];
        var newPwd = formCol["NewPassword"];
        await _authService.ChangePassword(currentPwd, newPwd);
        _notyfService.Success(Messages.SaveSuccessfully);
        return Json(null);
    }
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [HttpPost]
    [HttpGet]
    public IActionResult Error([FromForm] string message)
    {
        string? redirectUrl = null;
        var errCode = string.Empty;
        if (Request.QueryString.HasValue)
        {
            var values = new StringValues();
            if (Request.Query.TryGetValue("code", out values))
            {
                errCode = values.FirstOrDefault() ?? string.Empty;
            }
            if (Request.Query.TryGetValue("redirectUrl", out values))
            {
                if (!string.IsNullOrEmpty(errCode))
                {
                    _toastMsgService.AddError("", errCode);
                }
                redirectUrl = values.FirstOrDefault();
                return Redirect(redirectUrl!);
            }
        }
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            Message = message,
            BackUrl = redirectUrl ?? string.Format("/?redirectUrl={0}", Request.Headers["Referer"].ToString())
        });
    }
}