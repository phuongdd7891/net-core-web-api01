using System.Security.Claims;
using System.Text;
using CoreLibrary.Repository;
using IdentityMongo.Models;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using WebApi.Models.Requests;

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
                    if (item.Value.Contains(role.ToString()))
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

    private SigningCredentials CreateSigningCredentials() =>
        new SigningCredentials(
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["Jwt:Secret"]!)
            ),
            SecurityAlgorithms.HmacSha256Signature
        );
}