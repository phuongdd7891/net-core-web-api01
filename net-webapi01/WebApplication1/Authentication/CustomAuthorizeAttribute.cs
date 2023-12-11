using System.Security.Claims;
using CoreLibrary.Repository;
using IdentityMongo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace WebApplication1.Authentication;

public class CustomAuthorizeFilter : IAsyncAuthorizationFilter
{
    public const string API_KEY_HEADER = "ApiKey";
    private readonly RedisRepository _redisRepository;
    private readonly string _requestAction;

    public CustomAuthorizeFilter(
        RedisRepository redisRepository,
        string requestAction
    )
    {
        _redisRepository = redisRepository;
        _requestAction = requestAction;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // skip authorization if action is decorated with [AllowAnonymous] attribute
        var allowAnonymous = context.ActionDescriptor.EndpointMetadata.OfType<AllowAnonymousAttribute>().Any();
        if (allowAnonymous)
            return;

        // authorization
        if (!context.HttpContext.Request.Headers.ContainsKey(API_KEY_HEADER))
        {
            context.Result = new JsonResult(new ErrorResponse {
                Message = "Header key Not Found"
            }) { StatusCode = StatusCodes.Status401Unauthorized };
            return;
        }
        string apiKeyToValidate = context.HttpContext.Request.Headers[API_KEY_HEADER]!;

        if (!context.HttpContext.Request.Query.ContainsKey("u"))
        {
            context.Result = new JsonResult(new ErrorResponse {
                Message = "Username not found"
            }) { StatusCode = StatusCodes.Status401Unauthorized };
            return;
        }
        string username = context.HttpContext.Request.Query["u"]!;

        var apiKey = await _redisRepository.HasKey(username);
        if (!apiKey)
        {
            context.Result = new JsonResult(new ErrorResponse {
                Message = "User key not found"
            }) { StatusCode = StatusCodes.Status401Unauthorized };
            return;
        }
        else
        {
            var keyValue = await _redisRepository.Get(username);
            if (apiKeyToValidate != keyValue)
            {
                context.Result = new JsonResult(new ErrorResponse {
                    Message = "Invalid api key"
                }) { StatusCode = StatusCodes.Status401Unauthorized };
                return;
            }
        }
        var hashKey = $"{username}:{apiKeyToValidate}";
        var user = await _redisRepository.GetEntity<ApplicationUser>(hashKey);
        if (user == null)
        {
            context.Result = new ForbidResult(API_KEY_HEADER);
            return;
        }
        if (string.IsNullOrEmpty(_requestAction))
        {
            return;
        }
        var roles = await _redisRepository.GetHashByField<List<string>>(Const.ROLE_ACTION_KEY, _requestAction);
        if ((roles?.Count > 0 && !roles.Any(a => user!.Roles.Contains(Guid.Parse(a)))) || roles == null)
        {
            context.Result = new JsonResult(new ErrorResponse {
                Message = "Access denied"
            }) { StatusCode = StatusCodes.Status403Forbidden };
            return;
        }
    }
}

public class CustomAuthorizeAttribute : TypeFilterAttribute
{
    public CustomAuthorizeAttribute(
        string? requestAction = null
    ) : base(typeof(CustomAuthorizeFilter))
    {
        Arguments = new object[] {
            requestAction ?? ""
        };
    }
}