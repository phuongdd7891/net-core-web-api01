using System.Security.Claims;
using System.Text.Json;
using CoreLibrary.Repository;
using IdentityMongo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using WebApi.Models.Admin;
using WebApi.Services;

namespace WebApplication1.Authentication;

public class CustomAuthorizeFilter : IAsyncAuthorizationFilter
{
    public const string API_KEY_HEADER = "ApiKey";
    private readonly RedisRepository _redisRepository;
    private readonly JwtService _jwtService;
    private readonly string _requestAction;
    private readonly bool _isSystem;

    public CustomAuthorizeFilter(
        RedisRepository redisRepository,
        JwtService jwtService,
        string requestAction,
        bool isSystem
    )
    {
        _redisRepository = redisRepository;
        _requestAction = requestAction;
        _jwtService = jwtService;
        _isSystem = isSystem;
    }

    private JsonResult GetJsonResult(DataResponse<string> data)
    {
        var jsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            }
        };
        return new JsonResult(data, jsonSettings)
        {
            StatusCode = StatusCodes.Status401Unauthorized
        };
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // skip authorization if action is decorated with [AllowAnonymous] attribute
        var allowAnonymous = context.ActionDescriptor.EndpointMetadata.OfType<AllowAnonymousAttribute>().Any();
        if (allowAnonymous)
            return;

        if (!context.HttpContext.Request.Query.ContainsKey("u"))
        {
            context.Result = GetJsonResult(new DataResponse<string>
            {
                Data = "Username not found",
                Code = DataResponseCode.Unauthorized.ToString()
            });
            return;
        }
        string username = context.HttpContext.Request.Query["u"]!;

        // validate token
        if (!context.HttpContext.Request.Headers.ContainsKey("Authorization"))
        {
            context.Result = GetJsonResult(new DataResponse<string>
            {
                Data = "Header key Not Found",
                Code = DataResponseCode.Unauthorized.ToString()
            });
            return;
        }
        string? authHeader = context.HttpContext.Request.Headers["Authorization"];
        string token = authHeader?.Split(' ')[1] ?? string.Empty;
        bool validToken = await _jwtService.ValidateToken(token, username);
        if (!validToken)
        {
            context.Result = GetJsonResult(new DataResponse<string>
            {
                Data = "Invalid token",
                Code = DataResponseCode.Unauthorized.ToString()
            });
            return;
        }

        // check system user
        if (_isSystem)
        {
            var adminUser = await _redisRepository.GetEntity<AdminUser>(token);
            if (adminUser == null || !adminUser.IsSystem)
            {
                context.Result = GetJsonResult(new DataResponse<string>
                {
                    Data = "Account is not system user",
                    Code = DataResponseCode.Unauthorized.ToString()
                });
            }
            return;
        }

        // authorization
        if (string.IsNullOrEmpty(_requestAction))
        {
            return;
        }
        var user = await _redisRepository.GetEntity<ApplicationUser>($"{token}");
        var roles = await _redisRepository.GetHashByField<List<string>>(Const.ROLE_ACTION_KEY, _requestAction);
        if (roles?.Count == 0 || !roles!.Any(a => user!.Roles.Contains(Guid.Parse(a))) || user?.Roles.Count == 0)
        {
            context.Result = GetJsonResult(new DataResponse<string>
            {
                Data = "Access denied",
                Code = DataResponseCode.Unauthorized.ToString()
            });
            return;
        }
    }
}

public class CustomAuthorizeAttribute : TypeFilterAttribute
{
    public CustomAuthorizeAttribute(
        string? requestAction = null,
        bool isSystem = false
    ) : base(typeof(CustomAuthorizeFilter))
    {
        Arguments = new object[] {
            requestAction ?? "",
            isSystem
        };
    }
}