using AdminWeb.Services;
using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;

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
            try
            {
                var users = await _opService.GetUsers(skip, limit);
                return Json(new
                {
                    Total = users.Data!.Total,
                    List = users.Data.List
                });
            }
            catch (HttpRequestException ex)
            {
                Response.StatusCode = (int)ex.StatusCode!;
                return Json(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> LockUser(string username, bool locked)
        {
            try
            {
                var result = await _opService.SetLockUser(username, locked);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
    }
}
