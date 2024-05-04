using System.Diagnostics; using Microsoft.AspNetCore.Mvc; using AdminWeb.Models; using AdminWeb.Services; using AspNetCoreHero.ToastNotification.Abstractions; using Microsoft.Extensions.Primitives; using Microsoft.AspNetCore.Authorization;  namespace AdminWeb.Controllers;  public class HomeController : Controller {     private readonly ILogger<HomeController> _logger;     private readonly INotyfService _notyfService;     private readonly ToastMessageService _toastMsgService;

    public HomeController(ILogger<HomeController> logger, INotyfService notyfService, ToastMessageService toastMsgService)     {         _logger = logger;         _notyfService = notyfService;         _toastMsgService = toastMsgService;     }      public IActionResult Index()     {

        return View();     }      public IActionResult Privacy()     {         return View();     }      [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]     [HttpPost]     [HttpGet]     public IActionResult Error([FromForm] string message)     {         if (Request.QueryString.HasValue)
        {
            var values = new StringValues();
            if (Request.Query.TryGetValue("code", out values))
            {
                var errCode = values.FirstOrDefault() ?? string.Empty;
                if (Request.Query.TryGetValue("redirectUrl", out values))
                {
                    _toastMsgService.AddError("", errCode);
                    return Redirect(values.FirstOrDefault()!);
                }
                else
                {
                    message = errCode;
                }
            }
        }         return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
            Message = message,
            BackUrl = message.StartsWith("Token_") ? "/" : Request.Headers["Referer"].ToString()
    });     } } 