
using System.Dynamic;
using System.Security.Claims;
using System.Text.Encodings.Web;
using IdentityMongo.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using CoreLibrary.Repository;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Features;

namespace WebApplication1.Authentication;

public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string API_KEY_HEADER = "ApiKey";

    private readonly AppDBContext _context;
    private readonly RedisRepository _redisRepository;
    private string errMessage = "";

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock,
        AppDBContext context,
        RedisRepository redisRepository
    ) : base(options, logger, encoder, clock)
    {
        _context = context;
        _redisRepository = redisRepository;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey(API_KEY_HEADER))
        {
            return AuthenticateResult.Fail(errMessage = "Header key Not Found");
        }
        string apiKeyToValidate = Request.Headers[API_KEY_HEADER]!;

        if (!Request.Query.ContainsKey("u"))
        {
            return AuthenticateResult.Fail(errMessage = "Username Not Found");
        }
        string username = Request.Query["u"]!;

        var apiKey = await _redisRepository.HasKey(username);
        if (!apiKey)
        {
            return AuthenticateResult.Fail(errMessage = "User key not found");
        }
        else
        {
            var keyValue = await _redisRepository.Get(username);
            if (apiKeyToValidate != keyValue)
            {
                return AuthenticateResult.Fail(errMessage = "Invalid key");
            }
        }
        var hashKey = $"{username}:{apiKeyToValidate}";
        var user = await _redisRepository.GetEntity<ApplicationUser>(hashKey);
        if (user == null)
        {
            return AuthenticateResult.Fail(errMessage = "Invalid user");
        }
        if (await ValidateRoleAction(user.Roles) == false)
        {
            return AuthenticateResult.Fail(errMessage = "Access denied");
        }
        return AuthenticateResult.Success(await CreateTicket(user));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 401;

        if (!string.IsNullOrEmpty(errMessage))
        {
            Response.WriteAsync(errMessage);
        }

        return Task.CompletedTask;
    }

    private async Task<AuthenticationTicket> CreateTicket(ApplicationUser user)
    {
        var claims = new[] {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName!),
            new Claim(ClaimTypes.Email, user.Email!)
        };
        if (user.Roles != null)
        {
            var roleDict = await _redisRepository.GetHashEntity<string>(Const.userRolesKey);
            foreach (var role in user.Roles)
            {
                claims = claims.Append(new Claim(ClaimTypes.Role, roleDict[role.ToString()])).ToArray();
            }
        }
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return ticket;
    }

    private async Task<bool> ValidateRoleAction(List<Guid> userRoles)
    {
        var roles = await _redisRepository.GetHashByField<List<string>>(Const.roleActionKey, string.Format("[{0}]{1}", Request.Method.ToLower(), Request.Path));
        return roles == null || roles.Count == 0 || roles.Any(a => userRoles.Contains(Guid.Parse(a)));
    }
}