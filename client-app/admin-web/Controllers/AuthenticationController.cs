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
        private readonly AuthService _authService;
        private readonly INotyfService _notyfService;

        public AuthenticationController(
            INotyfService notyfService,
            AuthService authService
        )
        {
            _notyfService = notyfService;
            _authService = authService;
        }

        [Route("roles")]
        public async Task<IActionResult> Role()
        {
            var roles = await _authService.GetUserRoles();
            var actions = await _authService.GetUserActions();
            ViewBag.actions = actions.Data;
            return View("role", roles.Data);
        }

        [HttpPost]
        [Route("create")]
        public async Task<IActionResult> Create(IFormCollection formCol)
        {
            var actions = formCol["RoleActs"];
            var roleName = formCol["Name"];
            var customerId = formCol["CustomerId"];
            var result = await _authService.CreateRole(roleName!, customerId);
            if (actions.Count > 0)
            {
                await _authService.AddRoleActions(result.Data!, actions!);
            }
            _notyfService.Success(Messages.SaveSuccessfully);
            return RoleListPartial();
        }

        [HttpPost]
        [Route("edit")]
        public async Task<IActionResult> Edit(IFormCollection formCol)
        {
            var actions = formCol["RoleActs"];
            var roleId = formCol["Id"];
            var roleName = formCol["Name"];
            var customerId = formCol["CustomerId"];
            await _authService.EditRole(roleId!, roleName!, customerId);
            await _authService.AddRoleActions(roleId!, actions!);
            _notyfService.Success(Messages.SaveSuccessfully);
            return RoleListPartial();
        }

        [HttpPost]
        [Route("delete")]
        public async Task<IActionResult> Delete(IFormCollection formCol)
        {
            var roleId = formCol["Id"];
            await _authService.DeleteRole(roleId!);
            _notyfService.Success(Messages.SaveSuccessfully);
            return RoleListPartial();
        }

        private IActionResult RoleListPartial()
        {
            var roles = _authService.GetUserRoles();
            roles.ConfigureAwait(false);
            return PartialView("_RoleList", roles.Result.Data);
        }
    }
}
