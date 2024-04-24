using AdminWeb.Services;
using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;

namespace AdminWeb.Controllers
{
    [Route("[controller]")]
    [Authorize]
    public class AuthenticationController : Controller
    {
        private readonly OperationService _opService;
        private readonly INotyfService _notyfService;

        public AuthenticationController(
            INotyfService notyfService,
            OperationService opService
        )
        {
            _notyfService = notyfService;
            _opService = opService;
        }

        [Route("roles")]
        public async Task<IActionResult> Role()
        {
            var roles = await _opService.GetUserRoles();
            var actions = await _opService.GetRequestActions();
            ViewBag.actions = actions.Data;
            return View("role", roles.Data);
        }

        [HttpPost]
        [Route("edit")]
        public async Task<IActionResult> Edit(IFormCollection formCol)
        {
            var actions = formCol["RoleActs"];
            var role = formCol["Name"];
            await _opService.AddRoleActions(role!, actions!);
            _notyfService.Success(Messages.SaveSuccessfully);
            var roles = await _opService.GetUserRoles();
            return PartialView("_RoleList", roles.Data);
        }
    }
}
