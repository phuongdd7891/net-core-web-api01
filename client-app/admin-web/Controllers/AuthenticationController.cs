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
            var actions = await _opService.GetUserActions();
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
            var result = await _opService.CreateRole(roleName!, customerId);
            if (actions.Count > 0)
            {
                await _opService.AddRoleActions(result.Data!, actions!);
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
            await _opService.EditRole(roleId!, roleName!, customerId);
            await _opService.AddRoleActions(roleId!, actions!);
            _notyfService.Success(Messages.SaveSuccessfully);
            return RoleListPartial();
        }

        [HttpPost]
        [Route("delete")]
        public async Task<IActionResult> Delete(IFormCollection formCol)
        {
            var roleId = formCol["Id"];
            await _opService.DeleteRole(roleId!);
            _notyfService.Success(Messages.SaveSuccessfully);
            return RoleListPartial();
        }

        private IActionResult RoleListPartial()
        {
            var roles = _opService.GetUserRoles();
            roles.ConfigureAwait(false);
            return PartialView("_RoleList", roles.Result.Data);
        }
    }
}
