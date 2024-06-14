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
        private readonly CustomerService _customerService;
        private readonly INotyfService _notyfService;
        public CustomersController(
            INotyfService notyfService,
            CustomerService customerService
        )
        {
            _notyfService = notyfService;
            _customerService = customerService;
        }

        [Authorize]
        public IActionResult Index()
        {
            return View();
        }

        [Route("getCustomers")]
        public async Task<IActionResult> GetCustomers()
        {
            var users = await _customerService.GetCustomers();
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
                await _customerService.CreateCustomer(model);
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
            var user = await _customerService.GetCustomer(username);
            return View(user.Data);
        }

        [Authorize]
        [HttpPost]
        [Route("edit/{username}")]
        public async Task<IActionResult> Edit([FromForm] CustomerViewModel model)
        {
            try
            {
                await _customerService.UpdateCustomer(model);
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
