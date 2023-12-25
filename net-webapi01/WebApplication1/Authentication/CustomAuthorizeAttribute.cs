using System.Security.Claims;
using System.Text.Json;
using CoreLibrary.Repository;
using IdentityMongo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

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

        // authorization
        if (!context.HttpContext.Request.Headers.ContainsKey(API_KEY_HEADER))
        {
            context.Result = GetJsonResult(new DataResponse<string>
            {
                Data = "Header key Not Found",
                Code = StatusCodes.Status401Unauthorized
            });
            return;
        }
        string apiKeyToValidate = context.HttpContext.Request.Headers[API_KEY_HEADER]!;

        if (!context.HttpContext.Request.Query.ContainsKey("u"))
        {
            context.Result = GetJsonResult(new DataResponse<string>
            {
                Data = "Username not found",
                Code = StatusCodes.Status401Unauthorized
            });
            return;
        }
        string username = context.HttpContext.Request.Query["u"]!;

        var apiKey = await _redisRepository.HasKey(username);
        if (!apiKey)
        {
            context.Result = GetJsonResult(new DataResponse<string>
            {
                Data = "User key not found",
                Code = StatusCodes.Status401Unauthorized
            });
            return;
        }
        else
        {
            var keyValue = await _redisRepository.Get(username);
            if (apiKeyToValidate != keyValue)
            {
                context.Result = GetJsonResult(new DataResponse<string>
                {
                    Data = "Invalid api key",
                    Code = StatusCodes.Status401Unauthorized
                });
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
            context.Result = GetJsonResult(new DataResponse<string>
            {
                Data = "Access denied",
                Code = StatusCodes.Status403Forbidden
            });
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