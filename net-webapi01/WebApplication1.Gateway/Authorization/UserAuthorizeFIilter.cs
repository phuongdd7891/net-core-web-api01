using CoreLibrary.Const;
using CoreLibrary.Models;
using CoreLibrary.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ILogger = Serilog.ILogger;

namespace Gateway.Authorization;

public class UserAuthorizeFilter : IAsyncAuthorizationFilter
{
    private readonly RedisRepository _redisRepository;
    private readonly IUserIdentity _userIdentity;
    private readonly ILogger _logger;
    private readonly DataResponse<string> accessDeniedResponse = new DataResponse<string>
    {
        Data = "Access denied"
    };

    public UserAuthorizeFilter(
        RedisRepository redisRepository,
        IUserIdentity userIdentity,
        ILogger logger
    )
    {
        _redisRepository = redisRepository;
        _userIdentity = userIdentity;
        _logger = logger;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (!context.HttpContext.Request.Query.ContainsKey("u"))
        {
            context.Result = new JsonResult(accessDeniedResponse)
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }
        string username = context.HttpContext.Request.Query["u"]!;
        if (username != _userIdentity.Username)
        {
            context.Result = new JsonResult(accessDeniedResponse)
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }

        if (_userIdentity.UserRoles?.Length == 0 || await _redisRepository.HasKey(username) == false)
        {
            context.Result = new JsonResult(accessDeniedResponse)
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }
        var action = context.HttpContext.Request.Path;
        var userCache = await _redisRepository.GetEntity<UserViewModel>(username);
        if (userCache == null || userCache.RoleIds == null || userCache.RoleIds.Count == 0)
        {
            context.Result = new JsonResult(accessDeniedResponse)
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }
        var actions = await _redisRepository.GetHashEntity<List<string>>(Const.ROLE_ACTION_KEY);
        var valid = _userIdentity.UserRoles!.Any(role => Guid.TryParse(role, out Guid result) && userCache.RoleIds!.Contains(result) && actions.ContainsKey(role) && actions[role].Contains(action));
        if (!valid)
        {
            context.Result = new JsonResult(new DataResponse<string>
            {
                Data = $"Access denied to {action}"
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return;
        }
    }
}

public class UserAuthorizeAttribute : TypeFilterAttribute
{
    public UserAuthorizeAttribute() : base(typeof(UserAuthorizeFilter))
    {

    }
}