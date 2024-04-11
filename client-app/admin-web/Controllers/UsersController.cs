using AdminWeb.Models;
using AdminWeb.Services;
using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Sockets;

namespace AdminWeb.Controllers
{
    [Route("[controller]")]
    public class UsersController : BaseController
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
            var roles = await _opService.GetUserRoles();
            TempData["Roles"] = JsonConvert.SerializeObject(roles.Data);
            ViewBag.cid = customerId;
            return View(user.Data);
        }

        [Authorize]
        [HttpPost]
        [Route("edit/{username}/{customerId?}")]
        public async Task<IActionResult> Edit([FromForm] UserViewModel model, string[] usrRoles, IFormCollection formCol)
        {
            var customerId = formCol["cid"];
            try
            {
                await _opService.UpdateUser(model);
                await _opService.AddUserToRoles(model.Username, usrRoles);
                await _opService.SetLockUser(model.Username, model.IsLocked);
                _notyfService.Success(Messages.SaveSuccessfully);
            }
            catch (Exception ex)
            {
                _notyfService.Error(ex.InnerException?.Message ?? ex.Message);
                ViewBag.cid = customerId;
                return View(model);
            }
            return RedirectToAction("index", new { customerId });
        }

        [Authorize]
        [Route("create/{customerId?}")]
        public async Task<IActionResult> Create(string? customerId)
        {
            var roles = await _opService.GetUserRoles();
            TempData["Roles"] = JsonConvert.SerializeObject(roles.Data);
            ViewBag.cid = customerId;
            var vm = new UserViewModel { CustomerId = customerId };
            return View(vm);
        }

        [Authorize]
        [HttpPost]
        [Route("create/{customerId?}")]
        public async Task<IActionResult> Create([FromForm] UserViewModel model, string[] usrRoles, IFormCollection formCol)
        {
            var customerId = formCol["cid"];
            try
            {
                await _opService.CreateUser(model);
                await _opService.AddUserToRoles(model.Username, usrRoles);
                _notyfService.Success(Messages.CreateSuccessfully);
            }
            catch (Exception ex)
            {
                _notyfService.Error(ex.InnerException?.Message ?? ex.Message);
                model.Roles = usrRoles;
                ViewBag.cid = customerId;
                return View(model);
            }
            return RedirectToAction("index", new { customerId });
        }
    }
}
