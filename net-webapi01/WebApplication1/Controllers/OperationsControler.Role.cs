using System.ComponentModel;
using System.Reflection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using WebApi.Models;
using WebApi.Models.Requests;
using WebApi.Authentication;

namespace WebApi.Controllers;

public partial class OperationsController
{
    [HttpPost("create-role")]
    public async Task<IActionResult> CreateRole([FromBody] ApplicationRoleRequest request)
    {
        IdentityResult result = await _roleManager.CreateAsync(new ApplicationRole()
        {
            Name = request.StoreName,
            CustomerId = string.IsNullOrEmpty(request.CustomerId) ? null : request.CustomerId,
        });
        ErrorStatuses.ThrowInternalErr(result.Errors.FirstOrDefault()?.Code.ToErrDescription(request.Name) ?? "", !result.Succeeded);
        await _cacheService.LoadUserRoles();
        var createdRole = await _roleManager.FindByNameAsync(request.StoreName);
        return Ok(new DataResponse
        {
            Data = createdRole!.Id.ToString()
        });
    }

    [HttpPost("edit-role")]
    public async Task<IActionResult> EditRole([FromBody] ApplicationRoleRequest request)
    {
        ErrorStatuses.ThrowInternalErr("Invalid request", request == null || string.IsNullOrEmpty(request.Id));
        var role = await _roleManager.FindByIdAsync(request!.Id!);
        ErrorStatuses.ThrowInternalErr("Invalid role", role == null);
        role!.CustomerId = request.CustomerId;
        var tasks = new List<Task<IdentityResult>>
        {
            _roleManager.UpdateAsync(role),
            _roleManager.SetRoleNameAsync(role, request.StoreName)
        };
        var results = await Task.WhenAll(tasks);
        if (results.Any(a => !a.Succeeded))
        {
            var result = results.FirstOrDefault(a => !a.Succeeded);
            ErrorStatuses.ThrowInternalErr(result!.Errors.FirstOrDefault()?.Description ?? "", !result.Succeeded);
        }
        await _cacheService.LoadUserRoles(true);
        return Ok();
    }

    [HttpPost("add-user-roles")]
    [AdminAuthorize(true, true)]
    public async Task<IActionResult> AddUserRoles(UserRolesRequest req)
    {
        var user = await _userManager.FindByNameAsync(req.Username);
        ErrorStatuses.ThrowNotFound("User not found", user == null);
        if (user!.Roles.Count > 0)
        {
            var roles = _roleManager.Roles.Where(a => user.Roles.Contains(a.Id)).Select(a => a.Name).ToList();
            var result = await _userManager.RemoveFromRolesAsync(user, roles!);
            ErrorStatuses.ThrowInternalErr(result.Errors.FirstOrDefault()?.Description ?? "Remove role fail", !result.Succeeded);
        }
        if (req.Roles.Length > 0)
        {
            var addResult = await _userManager.AddToRolesAsync(user!, req.Roles);
            ErrorStatuses.ThrowInternalErr(addResult.Errors.FirstOrDefault()?.Description ?? "Add role fail", !addResult.Succeeded);
        }
        await _cacheService.UpdateUser(new UserViewModel(user));
        return Ok();
    }

    [HttpGet("user-roles")]
    [AdminAuthorize(true, true)]
    public async Task<DataResponse<List<GetRolesReply>>> GetUserRoles([FromQuery] string? customerId)
    {
        var roleActions = await _roleActionRepository.GetAll();
        var customerUsers = await _adminService.ListUsers(Profile!.IsCustomer);
        var result = _roleManager.Roles.Where(a => string.IsNullOrEmpty(a.CustomerId) || a.CustomerId == customerId || Profile.IsSystem).ToList()
            .GroupJoin(roleActions, a => Convert.ToString(a.Id), x => x.RoleId, (a, x) => new { Role = a, Action = x })
            .GroupJoin(customerUsers, a => a.Role.CustomerId, x => x.Id, (a, x) => new { a.Role, RoleAct = a.Action, Customers = x })
            .SelectMany(a => a.Customers.DefaultIfEmpty(), (a, x) =>
            {
                return new GetRolesReply
                {
                    Id = a.Role.Id,
                    Name = a.Role.Name,
                    Actions = a.RoleAct.SelectMany(x => x.Actions ?? new List<string>()).ToList(),
                    CustomerId = a.Role.CustomerId,
                    CustomerName = x?.FullName
                };
            }).ToList();
        return new DataResponse<List<GetRolesReply>>
        {
            Data = result
        };
    }

    [HttpPost("add-role-actions")]
    [AdminAuthorize(true)]
    public async Task<IActionResult> AddRoleActions(RoleActionRequest request)
    {
        await _roleActionRepository.AddActionsToRole(request.RoleId, request.Actions, User.Identity!.Name!);
        await _cacheService.LoadRoleActions();
        return Ok();
    }

    [HttpGet("user-actions")]
    [AdminAuthorize(true)]
    [ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult GetUserActions()
    {
        var endpoints = _endpointSources.SelectMany(es => es.Endpoints);
        var result = endpoints.Where(e => !string.IsNullOrEmpty(string.Format("{0}", e.Metadata.GetMetadata<UserAuthorizeAttribute>()))).Select(
            e =>
            {
                var controller = e.Metadata
                    .OfType<ControllerActionDescriptor>()
                    .FirstOrDefault();
                var action = controller != null
                    ? $"{controller.ControllerName}.{controller.ActionName}"
                    : null;
                var controllerMethod = controller != null
                    ? $"{controller.ControllerTypeInfo.FullName}:{controller.MethodInfo.Name}"
                    : null;
                return new UserActionResponse
                {
                    Method = e.Metadata.OfType<HttpMethodMetadata>().FirstOrDefault()?.HttpMethods?[0],
                    Action = action,
                    ControllerMethod = controllerMethod,
                    Description = controller?.MethodInfo.GetCustomAttribute<DescriptionAttribute>()?.Description ?? "",
                };
            }).ToList();
        return Ok(new DataResponse<List<UserActionResponse>>
        {
            Data = result
        });
    }

    [HttpPost("delete-role")]
    [AdminAuthorize(true)]
    public async Task<IActionResult> DeleteRole([FromBody] string id)
    {
        ErrorStatuses.ThrowBadRequest("Invalid request", string.IsNullOrEmpty(id));
        var role = await _roleManager.FindByIdAsync(id);
        ErrorStatuses.ThrowNotFound("Role not found", role == null);
        var result = await _roleManager.DeleteAsync(role!);
        if (result.Succeeded)
        {
            var roleName = role!.Name!;
            var users = await _userManager.GetUsersInRoleAsync(roleName);
            if (users?.Count > 0)
            {
                var tasks = new List<Task<IdentityResult>>();
                var userTasks = new List<Task>();
                foreach (var user in users)
                {
                    tasks.Add(_userManager.RemoveFromRoleAsync(user, roleName));
                    userTasks.Add(_cacheService.UpdateUser(new UserViewModel(user)));
                }
                var results = await Task.WhenAll(tasks);
                if (results.Any(a => !a.Succeeded))
                {
                    var error = results.FirstOrDefault(a => !a.Succeeded);
                    var index = results.ToList().IndexOf(error!);
                    ErrorStatuses.ThrowInternalErr(error!.Errors.FirstOrDefault()?.Description ?? string.Format("Remove user {0} from role {1} unsuccessfully", users[index].UserName, roleName), error != null);
                }
                await Task.WhenAll(userTasks);
            }
            await _roleActionRepository.DeleteByRoleId(id);
        }
        else
        {
            ErrorStatuses.ThrowInternalErr(result.Errors.FirstOrDefault()?.Description ?? "Delete role fail", !result.Succeeded);
        }
        await _cacheService.LoadUserRoles(true);
        await _cacheService.LoadRoleActions(true);
        return Ok();
    }
}