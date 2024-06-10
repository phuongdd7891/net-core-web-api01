using System.Security.Claims;
using CoreLibrary.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using WebApi.Models;

namespace WebApplication1.Authentication;

public class UserAuthorizeFilter : IAsyncAuthorizationFilter
{
    private readonly RedisRepository _redisRepository;
    private readonly DataResponse<string> accessDeniedResponse = new DataResponse<string>
    {
        Data = "Access denied",
        Code = DataResponseCode.Unauthorized.ToString()
    };

    public UserAuthorizeFilter(
        RedisRepository redisRepository
    )
    {
        _redisRepository = redisRepository;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (!context.HttpContext.Request.Query.ContainsKey("u"))
        {
            context.Result = Helpers.GetUnauthorizedResult(new DataResponse<string>
            {
                Data = "Username not found",
                Code = DataResponseCode.Unauthorized.ToString()
            });
            return;
        }
        string username = context.HttpContext.Request.Query["u"]!;
        var claimRole = context.HttpContext.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.Role)?.Value;
        if (string.IsNullOrEmpty(claimRole) || await _redisRepository.HasKey(username) == false)
        {
            context.Result = Helpers.GetUnauthorizedResult(accessDeniedResponse);
            return;
        }
        var userCache = await _redisRepository.GetEntity<UserViewModel>(username);
        var endpoint = context.HttpContext.GetEndpoint();
        var controller = endpoint?.Metadata
            .OfType<ControllerActionDescriptor>()
            .FirstOrDefault();
        var action = controller != null
            ? $"{controller.ControllerName}.{controller.ActionName}"
            : null;
        if (!string.IsNullOrEmpty(action))
        {
            var actions = await _redisRepository.GetHashEntity<List<string>>(Const.ROLE_ACTION_KEY);
            var roles = claimRole.Split(',');
            var valid = roles.Any(role => userCache!.RoleIds!.Contains(Guid.Parse(role)) && actions.ContainsKey(role) && actions[role].Contains(action));
            if (!valid)
            {
                context.Result = Helpers.GetUnauthorizedResult(accessDeniedResponse);
                return;
            }
        }
    }
}

public class UserAuthorizeAttribute : TypeFilterAttribute
{
    public UserAuthorizeAttribute() : base(typeof(UserAuthorizeFilter))
    {

    }
}