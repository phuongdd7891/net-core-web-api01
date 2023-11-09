using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CoreLibrary.Repository;
using IdentityMongo.Models;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using WebApplication1.ResourceModels;

namespace WebApi.Services;

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
        var roles = await _redisRepository.GetHashEntity<string>(Const.USER_ROLES_KEY);
        var tokenHandler = new JsonWebTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(CreateClaims(user, roles)),
            Audience = _configuration["Jwt:Audience"],
            Issuer = _configuration["Jwt:Issuer"],
            Expires = expiration,
            SigningCredentials = CreateSigningCredentials()
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return new AuthenticationResponse
        {
            Token = token,
            Expiration = expiration
        };
    }

    private Claim[] CreateClaims(ApplicationUser user, Dictionary<string, string> roles)
    {
        var claims = new[] {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName!),
            new Claim(ClaimTypes.Email, user.Email!)
        };
        if (user.Roles != null)
        {
            foreach (var role in user.Roles)
            {
                claims = claims.Append(new Claim(ClaimTypes.Role, roles[role.ToString()])).ToArray();
            }
        }
        return claims;
    }

    private SigningCredentials CreateSigningCredentials() =>
        new SigningCredentials(
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:Secret"]!)
            ),
            SecurityAlgorithms.HmacSha256Signature
        );
}