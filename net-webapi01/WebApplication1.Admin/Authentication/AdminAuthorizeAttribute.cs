using WebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using WebApi.Services;
using Newtonsoft.Json;
using System.Security.Claims;

namespace WebApi.Authentication;

public class AdminAuthorizeFilter : IAsyncAuthorizationFilter
{
    public const string API_KEY_HEADER = "ApiKey"; 
    private readonly JwtService _jwtService;
    private readonly bool _isSystem;
    private readonly bool _isCustomer;

    public AdminAuthorizeFilter(
        JwtService jwtService,
        bool isSystem,
        bool isCustomer
    )
    {
        _jwtService = jwtService;
        _isSystem = isSystem;
        _isCustomer = isCustomer;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // skip authorization if action is decorated with [AllowAnonymous] attribute
        var allowAnonymous = context.ActionDescriptor.EndpointMetadata.OfType<AllowAnonymousAttribute>().Any();
        if (allowAnonymous)
            return;

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

        // validate token
        if (!context.HttpContext.Request.Headers.ContainsKey("Authorization"))
        {
            context.Result = Helpers.GetUnauthorizedResult(new DataResponse<string>
            {
                Data = "Header key Not Found",
                Code = DataResponseCode.Unauthorized.ToString()
            });
            return;
        }
        string? authHeader = context.HttpContext.Request.Headers["Authorization"];
        string token = authHeader?.Split(' ')[1] ?? string.Empty;
        var validateResult = await _jwtService.ValidateToken(token, username);
        if (!validateResult.Success)
        {
            context.Result = Helpers.GetUnauthorizedResult(new DataResponse<string>
            {
                Data = validateResult.Message,
                Code = validateResult.Code
            });
            return;
        }

        // check system user
        if (_isSystem || _isCustomer)
        {
            var userData = validateResult.Claims?.Where(a => a.Type == ClaimTypes.UserData).FirstOrDefault()?.Value ?? string.Empty;
            var adminUser = !string.IsNullOrEmpty(userData) ? JsonConvert.DeserializeObject<AdminProfile>(userData) : null;
            if (adminUser == null || (adminUser.IsSystem != _isSystem && adminUser.IsCustomer != _isCustomer))
            {
                context.Result = Helpers.GetUnauthorizedResult(new DataResponse<string>
                {
                    Data = $"Access denied to \"{context.HttpContext.Request.Path}\"",
                    Code = DataResponseCode.Unauthorized.ToString()
                });
            }
            return;
        }
    }
}

public class AdminAuthorizeAttribute : TypeFilterAttribute
{
    public AdminAuthorizeAttribute(
        bool isSystem = false,
        bool isCustomer = false
    ) : base(typeof(AdminAuthorizeFilter))
    {
        Arguments = new object[] {
            isSystem,
            isCustomer
        };
    }
}