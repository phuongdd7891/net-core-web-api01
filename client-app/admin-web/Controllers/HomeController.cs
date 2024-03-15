using System.Diagnostics; using Microsoft.AspNetCore.Mvc; using AdminWeb.Models; using AdminWeb.Services; using Newtonsoft.Json; using Microsoft.AspNetCore.Authorization; using AdminWeb.Models.Response; using static System.Runtime.InteropServices.JavaScript.JSType; using AspNetCoreHero.ToastNotification.Abstractions; using Microsoft.Extensions.Primitives;  namespace AdminWeb.Controllers;  public class HomeController : Controller {     private readonly ILogger<HomeController> _logger;     private readonly INotyfService _notyfService;     private readonly ToastMessageService _toastMsgService;          public HomeController(ILogger<HomeController> logger, INotyfService notyfService, ToastMessageService toastMsgService)     {         _logger = logger;         _notyfService = notyfService;         _toastMsgService = toastMsgService;     }      public IActionResult Index()     {                  return View();     }      public IActionResult Privacy()     {         return View();     }      [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]     public IActionResult Error()     {         if (Request.QueryString.HasValue)
        {
            var values = new StringValues();
            if (Request.Query.TryGetValue("code", out values))
            {
                var errCode = values.FirstOrDefault() ?? string.Empty;
                _toastMsgService.AddError("", errCode);
                if (string.Compare(errCode, Const.ErrCode_InvalidToken) == 0)
                {
                    return RedirectToAction("login", "account");
                }
            }
        }         return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });     } } 