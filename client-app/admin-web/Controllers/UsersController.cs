using AdminWeb.Models;
using AdminWeb.Services;
using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.ComponentModel;

namespace AdminWeb.Controllers
{
    [Route("[controller]")]
    public class UsersController : Controller
    {
        private readonly OperationService _opService;
        private readonly INotyfService _notyfService;

        public UsersController(
            INotyfService notyfService,
            OperationService opService
        )
        {
            _notyfService = notyfService;
            _opService = opService;
        }

        [Authorize]
        [Route("{customerId?}")]
        public IActionResult Index(string? customerId)
        {
            ViewBag.cid = customerId;
            return View();
        }

        [Route("getUsers")]
        public async Task<IActionResult> GetUsers(int skip, int limit, string? customerId)
        {
            var users = await _opService.GetUsers(skip, limit, customerId);
            return Json(new
            {
                Total = users.Data!.Total,
                List = users.Data.List
            });
        }

        [HttpPost]
        public async Task<IActionResult> LockUser(string username, bool locked)
        {
            var result = await _opService.SetLockUser(username, locked);
            return Json(result);
        }

        [Authorize]
        [Route("edit/{username}/{customerId?}")]
        public async Task<IActionResult> Edit(string username, string? customerId)
        {
            var user = await _opService.GetUser(username);
            var roles = await _opService.GetUserRoles(customerId);
            TempData["Roles"] = JsonConvert.SerializeObject(roles.Data);
            return View(user.Data);
        }

        [Authorize]
        [HttpPost]
        [Route("edit/{username}/{customerId?}")]
        public async Task<IActionResult> Edit([FromForm] UserViewModel model, string[] usrRoles, IFormCollection formCol)
        {
            var customerId = model.CustomerId;
            try
            {
                await _opService.UpdateUser(model);
                var roles = usrRoles.Where(a => a.IndexOf("__") < 0 || a.StartsWith(string.Format("{0}__", customerId))).ToArray();
                await _opService.AddUserToRoles(model.Username, roles);
                await _opService.SetLockUser(model.Username, model.IsLocked);
                _notyfService.Success(Messages.SaveSuccessfully);
            }
            catch (Exception ex)
            {
                _notyfService.Error(ex.InnerException?.Message ?? ex.Message);
                model.Roles = usrRoles;
                return View(model);
            }
            return RedirectToAction("index", new { customerId = Request.RouteValues["customerId"] });
        }

        [Authorize]
        [Route("create/{customerId?}")]
        public async Task<IActionResult> Create(string? customerId)
        {
            var roles = await _opService.GetUserRoles(customerId);
            TempData["Roles"] = JsonConvert.SerializeObject(roles.Data);
            var vm = new UserViewModel { CustomerId = customerId };
            return View(vm);
        }

        [Authorize]
        [HttpPost]
        [Route("create/{customerId?}")]
        public async Task<IActionResult> Create([FromForm] UserViewModel model, string[] usrRoles, IFormCollection formCol)
        {
            var customerId = model.CustomerId;
            try
            {
                await _opService.CreateUser(model);
                var roles = usrRoles.Where(a => a.IndexOf("__") < 0 || a.StartsWith(string.Format("{0}__", customerId))).ToArray();
                await _opService.AddUserToRoles(model.Username, roles);
                _notyfService.Success(Messages.CreateSuccessfully);
            }
            catch (Exception ex)
            {
                _notyfService.Error(ex.InnerException?.Message ?? ex.Message);
                model.Roles = usrRoles;
                return View(model);
            }
            return RedirectToAction("index", new { customerId = Request.RouteValues["customerId"] });
        }
    }
}
