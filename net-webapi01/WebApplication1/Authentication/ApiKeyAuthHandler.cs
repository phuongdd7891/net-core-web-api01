
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
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

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
            if (string.Compare(apiKeyToValidate, keyValue) != 0)
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
        return AuthenticateResult.Success(await CreateTicket(user));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = 401;
        Response.ContentType = "application/json";
        if (!string.IsNullOrEmpty(errMessage))
        {
            var jsonSettings = new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy()
                }
            };
            Response.WriteAsync(JsonConvert.SerializeObject(new DataResponse<string>
            {
                Code = DataResponseCode.Unauthorized.ToString(),
                Data = errMessage
            }, jsonSettings));
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
            var roleValues = await _redisRepository.GetHashValues<List<string>>(Const.ROLE_ACTION_KEY);
            var roleList = roleValues.SelectMany(a => a!.Where(x => user.Roles.Contains(Guid.Parse(x))).Select(x => x)).DistinctBy(a => a).ToList();
            var roleAction = await _redisRepository.GetHashEntity<List<string>>(Const.ROLE_ACTION_KEY);
            foreach (var item in roleAction)
            {
                if (item.Value.Any(a => roleList.Contains(a)))
                {
                    claims = claims.Append(new Claim(ClaimTypes.Role, item.Key)).ToArray();
                }
            }
        }
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return ticket;
    }
}