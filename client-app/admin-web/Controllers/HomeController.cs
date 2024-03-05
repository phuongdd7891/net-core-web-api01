using System.Diagnostics; using Microsoft.AspNetCore.Mvc; using AdminWeb.Models; using AdminWeb.Services; using Newtonsoft.Json; using Microsoft.AspNetCore.Authorization; using AdminWeb.Models.Response; using static System.Runtime.InteropServices.JavaScript.JSType; using AspNetCoreHero.ToastNotification.Abstractions;  namespace AdminWeb.Controllers;  public class HomeController : Controller {     private readonly ILogger<HomeController> _logger;     private readonly INotyfService _notyfService;     private readonly BookService _bookService;      public HomeController(ILogger<HomeController> logger, BookService bookService, INotyfService notyfService)     {         _logger = logger;         _notyfService = notyfService;         _bookService = bookService;     }      public IActionResult Index()     {         return View();     }      public IActionResult Privacy()     {         return View();     }      [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]     public IActionResult Error()     {         return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });     }      [AllowAnonymous]     [Route("login")]     [HttpPost]     public async Task<IActionResult> Login(LoginModel loginModel)     {         try
        {
            var result = await _bookService.Login(loginModel.UserName, loginModel.Password);
            HttpContext.Session.SetString("Token", result!.Data!.Token);
            HttpContext.Session.SetString("Username", result.Data.Username);
        }
        catch (Exception ex)
        {             _notyfService.Error(ex.Message);             return View("Index", loginModel);
        }         return RedirectToAction("Index", "Users");     }      [Authorize]     public async Task<IActionResult> Logout()
    {
        var result = await _bookService.Logout();
        HttpContext.Session.Clear();
        return RedirectToAction("Index");
    } } 