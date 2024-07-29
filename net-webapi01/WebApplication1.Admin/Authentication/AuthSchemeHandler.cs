using System.Security.Claims;
using System.Text.Encodings.Web;
using Grpc.Core;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using WebApi.Models;
using WebApi.Services;

namespace WebApi.Authentication;

public class SessionTokenAuthSchemeHandler : AuthenticationHandler<JwtBearerOptions>
{
    private readonly JwtService _jwtService;
    private string errMessage = string.Empty;

    public SessionTokenAuthSchemeHandler(
        IOptionsMonitor<JwtBearerOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        JwtService jwtService
    ) : base(options, logger, encoder)
    {
        _jwtService = jwtService;
    }

    protected async override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Read the token from request headers/cookies
        // Check that it's a valid session, depending on your implementation
        if (!Request.Headers.ContainsKey("Username"))
        {
            return AuthenticateResult.Fail(errMessage = "Username not found");
        }
        if (!Request.Headers.ContainsKey("Authorization"))
        {
            return AuthenticateResult.Fail(errMessage = "Authorization key Not Found");
        }
        string? authHeader = Request.Headers["Authorization"];
        var token = authHeader?.Split(' ')[1] ?? string.Empty;
        var username = Convert.ToString(Request.Headers["Username"]) ?? string.Empty;
        var validateResult = await _jwtService.ValidateToken(token, username);
        if (!validateResult.IsOk)
        {
            return AuthenticateResult.Fail(errMessage = validateResult.Message);
        }
        var userData = validateResult.Claims?.Where(a => a.Type == ClaimTypes.UserData).FirstOrDefault()?.Value ?? string.Empty;
        var adminUser = !string.IsNullOrEmpty(userData) ? JsonConvert.DeserializeObject<AdminProfile>(userData) : null;
        if (adminUser == null || !adminUser.IsSystem)
        {
            return AuthenticateResult.Fail(errMessage = $"Access denied to \"{Request.Path}\"");
        }
        // If the session is valid, return success:
        var claims = validateResult.Claims;
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return AuthenticateResult.Success(ticket);
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        if (!string.IsNullOrEmpty(errMessage))
        {
            throw new RpcException(new Status(StatusCode.Unauthenticated, errMessage));
        }

        return Task.CompletedTask;
    }
}