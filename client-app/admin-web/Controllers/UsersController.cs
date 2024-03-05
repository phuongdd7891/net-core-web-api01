using AdminWeb.Services;
using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace AdminWeb.Controllers
{
    public class UsersController : Controller
    {
        private readonly BookService _bookService;
        private readonly INotyfService _notyfService;

        public UsersController(
            INotyfService notyfService,
            BookService bookService
        )
        {
            _notyfService = notyfService;
            _bookService = bookService;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> GetUsers(int skip, int limit)
        {
            try
            {
                var users = await _bookService.GetUsers(skip, limit);
                return Json(new
                {
                    Total = users.Data!.Total,
                    List = users.Data.List
                });
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }

        [HttpPost]
        public async Task<IActionResult> LockUser(string username, bool locked)
        {
            try
            {
                var result = await _bookService.SetLockUser(username, locked);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(ex.Message);
            }
        }
    }
}
