using System.ComponentModel;
using System.Reflection;
using Gateway.Models.Requests;
using Gateway.Models.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Gateway.Controllers;

public partial class UserController
{
    [HttpGet("user-actions")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult GetUserActions()
    {
        var actions = new List<UserActionResponse>();

        foreach (var dataSource in _endpointDataSources)
        {
            var endpoints = dataSource.Endpoints;

            foreach (var endpoint in endpoints)
            {
                if (endpoint is RouteEndpoint routeEndpoint)
                {
                    var controllerActionDescriptor = routeEndpoint.Metadata
                        .OfType<ControllerActionDescriptor>()
                        .FirstOrDefault();
                    var explorerSettingAttr = routeEndpoint.Metadata.GetMetadata<ApiExplorerSettingsAttribute>();
                    if (controllerActionDescriptor != null && (explorerSettingAttr == null || !explorerSettingAttr.IgnoreApi))
                    {
                        var actionId = controllerActionDescriptor.Properties["ActionId"] as string;
                        var httpMethods = routeEndpoint.Metadata
                            .OfType<HttpMethodMetadata>()
                            .FirstOrDefault()?.HttpMethods;

                        actions.Add(new UserActionResponse
                        {
                            ControllerMethod = $"{controllerActionDescriptor.ControllerTypeInfo.FullName}:{controllerActionDescriptor.MethodInfo.Name}",
                            Description = controllerActionDescriptor.MethodInfo.GetCustomAttribute<DescriptionAttribute>()?.Description,
                            Method = string.Join(", ", httpMethods ?? new List<string>()),
                            ActionId = actionId,
                            Route = routeEndpoint.RoutePattern.RawText
                        });
                    }
                }
            }
        }
        return Ok(new DataResponse<List<UserActionResponse>>
        {
            Data = actions
        });
    }

    [HttpPost("create-role")]
    public async Task<IActionResult> CreateRole([FromBody] ApplicationRoleRequest request)
    {
        var result = await _adminAuthClient.CreateUserRoleAsync(new Adminauthservice.CreateUserRoleRequest
        {
            Name = request.StoreName,
            CustomerId = string.IsNullOrEmpty(request.CustomerId) ? null : request.CustomerId,
        }, DefaultHeader);
        ErrorStatuses.ThrowInternalErr(result.Message, !string.IsNullOrEmpty(result.Message));
        await _cacheService.LoadUserRoles();

        return Ok(new DataResponse
        {
            Data = result.Id
        });
    }

    [HttpPost("add-role-actions")]
    public async Task<IActionResult> AddRoleActions(RoleActionRequest request)
    {
        var actionReq = new Adminauthservice.AddActionsToRoleRequest
        {
            RoleId = request.RoleId
        };
        var actions = new List<string>();
        foreach (var dataSource in _endpointDataSources)
        {
            var endpoints = dataSource.Endpoints;

            foreach (var endpoint in endpoints)
            {
                if (endpoint is RouteEndpoint routeEndpoint)
                {
                    var controllerActionDescriptor = routeEndpoint.Metadata
                        .OfType<ControllerActionDescriptor>()
                        .FirstOrDefault(a => request.ActionIds.Contains(a.Properties["ActionId"] as string));
                    if (controllerActionDescriptor != null)
                    {
                        actions.Add((controllerActionDescriptor.Properties["ActionId"] as string)!);
                    }
                }
            }
        }
        actionReq.Actions.AddRange(actions);
        await _adminAuthClient.AddActionsToRoleAsync(actionReq, DefaultHeader);
        await _cacheService.LoadRoleActions();
        return Ok();
    }

    [HttpPost("edit-role")]
    public async Task<IActionResult> EditRole([FromBody] ApplicationRoleRequest request)
    {
        ErrorStatuses.ThrowInternalErr("Invalid request", request == null || string.IsNullOrEmpty(request.Id));
        var result = await _adminAuthClient.UpdateUserRoleAsync(new Adminauthservice.UpdateUserRoleRequest
        {
            Id = request!.Id,
            Name = request.StoreName,
            CustomerId = string.IsNullOrEmpty(request.CustomerId) ? null : request.CustomerId,
        }, DefaultHeader);
        ErrorStatuses.ThrowInternalErr(result.Message, !string.IsNullOrEmpty(result.Message));
        await _cacheService.LoadUserRoles(true);
        return Ok();
    }

    [HttpPost("delete-role")]
    public async Task<IActionResult> DeleteRole([FromBody] string id)
    {
        ErrorStatuses.ThrowBadRequest("Invalid request", string.IsNullOrEmpty(id));
        var result = await _adminAuthClient.DeleteUserRoleAsync(new Adminauthservice.DeleteUserRoleRequest
        {
            Id = id
        }, DefaultHeader);
        ErrorStatuses.ThrowInternalErr(result.Message, !string.IsNullOrEmpty(result.Message));
        await _cacheService.LoadUserRoles(true);
        await _cacheService.LoadRoleActions(true);
        return Ok();
    }
}