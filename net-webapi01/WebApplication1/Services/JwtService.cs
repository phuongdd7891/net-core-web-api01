using System.Security.Claims;
using System.Text;
using CoreLibrary.Repository;
using WebApi.Models;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using WebApi.Models.Admin;
using WebApi.Models.Requests;
using System.Security.Cryptography;
using CoreLibrary.Helpers;
using NLog.LayoutRenderers;

namespace WebApi.Services;

public class ValidateTokenResult
{
    public string Code { get; set; } = DataResponseCode.Ok.ToString();
    public string Message { get; set; } = string.Empty;
    public bool IsOk
    {
        get => Code == DataResponseCode.Ok.ToString();
    }
}

public class JwtService
{
    private const int EXPIRATION_MINUTES = 24 * 60;

    private readonly IConfiguration _configuration;
    private readonly RedisRepository _redisRepository;

    public JwtService(
        IConfiguration configuration,
        RedisRepository redisRepository
    )
    {
        _configuration = configuration;
        _redisRepository = redisRepository;
    }

    public async Task<AuthenticationResponse> CreateToken(ApplicationUser user)
    {
        var expiration = DateTime.UtcNow.AddMinutes(EXPIRATION_MINUTES);
        var tokenHandler = new JsonWebTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(await CreateClaims(user)),
            Audience = _configuration["Jwt:Audience"],
            Issuer = _configuration["Jwt:Issuer"],
            Expires = expiration,
            SigningCredentials = CreateSigningCredentials()
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        await _redisRepository.SetEntity(user.UserName!, user, EXPIRATION_MINUTES);
        return new AuthenticationResponse
        {
            Token = token,
            Expiration = expiration,
            Username = user.UserName!
        };
    }

    private async Task<Claim[]> CreateClaims(ApplicationUser user)
    {
        var claims = new[] {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName!),
            new Claim(ClaimTypes.Email, user.Email!)
        };
        if (user.Roles != null)
        {
            var roleActions = await _redisRepository.GetHashEntity<string[]>(Const.ROLE_ACTION_KEY);
            var actions = new List<string>();
            foreach (var role in user.Roles)
            {
                foreach (var item in roleActions)
                {
                    if (item.Value.Contains(role.ToString()) && !actions.Contains(item.Key))
                    {
                        actions.Add(item.Key);
                    }
                }
            }
            foreach (var action in actions)
            {
                claims = claims.Append(new Claim(ClaimTypes.Role, action)).ToArray();
            }
        }
        return claims;
    }

    private byte[] GetSecretKey() => Encoding.UTF8.GetBytes(AESHelpers.Decrypt(_configuration["Jwt:Secret"]!));

    private SigningCredentials CreateSigningCredentials() =>
        new SigningCredentials(
            new SymmetricSecurityKey(
                GetSecretKey()
            ),
            SecurityAlgorithms.HmacSha256Signature
        );

    public async Task<AuthenticationResponse> CreateAdminToken(AdminUser user, Claim[]? claims = null)
    {
        var expiration = DateTime.UtcNow.AddMinutes(EXPIRATION_MINUTES);
        var tokenHandler = new JsonWebTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims ?? new[] {
                new Claim(ClaimTypes.NameIdentifier, user.Id!),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.UserData, user.ToClaimData())
            }),
            Audience = _configuration["Jwt:Audience"],
            Issuer = _configuration["Jwt:Issuer"],
            Expires = expiration,
            SigningCredentials = CreateSigningCredentials()
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        await _redisRepository.SetEntity(user.Username, user, EXPIRATION_MINUTES);
        return new AuthenticationResponse
        {
            Token = token,
            Expiration = expiration,
            Username = user.Username
        };
    }

    public async Task<ValidateTokenResult> ValidateToken(string token, string username)
    {
        var tokenResult = new ValidateTokenResult();
        var tokenHandler = new JsonWebTokenHandler();
        var result = await tokenHandler.ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            ValidateIssuer = true,
            ValidateAudience = true,
            IssuerSigningKey = new SymmetricSecurityKey(GetSecretKey()),
            ValidIssuer = _configuration["Jwt:Issuer"],
            ValidAudience = _configuration["Jwt:Audience"],
            ClockSkew = TimeSpan.Zero,
            ValidateLifetime = true
        });
        if (result.IsValid)
        {
            bool validUsername = result.ClaimsIdentity.FindFirst(c => c.Type == ClaimTypes.Name)?.Value == username;
            if (!validUsername)
            {
                tokenResult.Message = "Username not match";
            }
        }
        else
        {
            if (Type.Equals(result.Exception.GetType(), typeof(SecurityTokenExpiredException)))
            {
                tokenResult.Code = DataResponseCode.TokenExpired.ToString();
                tokenResult.Message = "Token expired";
            }
            else
            {
                tokenResult.Code = DataResponseCode.InvalidToken.ToString();
                tokenResult.Message = result.Exception.Message;
            }
        }
        return tokenResult;
    }

    public string GenerateRefreshToken()
    {
        var randomNumber = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }

    public async Task<ClaimsIdentity> GetClaimsFromToken(string token)
    {
        var tokenHandler = new JsonWebTokenHandler();
        var result = await tokenHandler.ValidateTokenAsync(token, new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            ValidateIssuer = true,
            ValidateAudience = true,
            IssuerSigningKey = new SymmetricSecurityKey(GetSecretKey()),
            ValidIssuer = _configuration["Jwt:Issuer"],
            ValidAudience = _configuration["Jwt:Audience"],
            ValidateLifetime = false
        });
        if (result.IsValid)
        {
            return result.ClaimsIdentity;
        }
        throw result.Exception;
    }
}