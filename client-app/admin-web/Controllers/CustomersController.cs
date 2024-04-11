using AdminWeb.Models;
using AdminWeb.Models.Response;
using AdminWeb.Services;
using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace AdminWeb.Controllers
{
    [Route("[controller]")]
    public class CustomersController : Controller
    {
        private readonly OperationService _opService;
        private readonly INotyfService _notyfService;
        public CustomersController(
            INotyfService notyfService,
            OperationService opService
        )
        {
            _notyfService = notyfService;
            _opService = opService;
        }

        [Authorize]
        public IActionResult Index()
        {
            return View();
        }

        [Route("getCustomers")]
        public async Task<IActionResult> GetCustomers()
        {
            var users = await _opService.GetCustomers();
            return Json(new
            {
                Total = users.Data!.Count,
                List = users.Data
            });
        }

        [Authorize]
        [Route("create")]
        public IActionResult Create()
        {
            return View(new CustomerViewModel());
        }

        [Authorize]
        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> Create([FromForm] CustomerViewModel model)
        {
            try
            {
                await _opService.CreateCustomer(model);
                _notyfService.Success(Messages.CreateSuccessfully);
            }
            catch (Exception ex)
            {
                _notyfService.Error(ex.InnerException?.Message ?? ex.Message);
                return View(model);
            }
            return RedirectToAction("index");
        }

        [Authorize]
        [Route("edit/{username}")]
        public async Task<IActionResult> Edit(string username)
        {
            var user = await _opService.GetCustomer(username);
            return View(user.Data);
        }

        [Authorize]
        [HttpPost]
        [Route("edit/{username}")]
        public async Task<IActionResult> Edit([FromForm] CustomerViewModel model)
        {
            try
            {
                await _opService.UpdateCustomer(model);
                _notyfService.Success(Messages.SaveSuccessfully);
            }
            catch (Exception ex)
            {
                _notyfService.Error(ex.InnerException?.Message ?? ex.Message);
                return View(model);
            }
            return RedirectToAction("index");
        }
    }
}
