using AdminWeb.Models;
using AdminWeb.Services;
using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

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
        [Route("")]
        public IActionResult Index()
        {
            return View();
        }

        [Route("getUsers")]
        public async Task<IActionResult> GetUsers(int skip, int limit)
        {
            var users = await _opService.GetUsers(skip, limit);
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
        [Route("edit/{username}")]
        public async Task<IActionResult> Edit(string username)
        {
            var user = await _opService.GetUser(username);
            var roles = await _opService.GetUserRoles();
            TempData["Roles"] = JsonConvert.SerializeObject(roles.Data);
            return View(user.Data);
        }

        [Authorize]
        [HttpPost]
        [Route("edit/{username}")]
        public async Task<IActionResult> Edit([FromForm] UserViewModel model, string[] usrRoles)
        {
            try
            {
                await _opService.UpdateUser(model);
                await _opService.AddUserToRoles(model.Username, usrRoles);
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
