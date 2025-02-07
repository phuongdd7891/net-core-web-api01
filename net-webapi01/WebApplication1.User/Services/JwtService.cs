using CoreLibrary.DataModels;
using CoreLibrary.Models;
using CoreLibrary.Repository;
using WebApplication1.User.Models;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Text;

namespace WebApplication1.User.Services;

public class JwtService
{
    private readonly TimeSpan expiryMinutes = TimeSpan.FromMinutes(24 * 60);

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
        var expiration = DateTime.UtcNow.Add(expiryMinutes);
        var tokenHandler = new JsonWebTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(CreateClaims(user)),
            Audience = _configuration["Jwt:Audience"],
            Issuer = _configuration["Jwt:Issuer"],
            Expires = expiration,
            SigningCredentials = CreateSigningCredentials()
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        await _redisRepository.SetEntity(user.UserName!, new UserViewModel(user), expiryMinutes);
        return new AuthenticationResponse
        {
            Token = token,
            Expiration = expiration,
            Username = user.UserName!
        };
    }

    private Claim[] CreateClaims(ApplicationUser user)
    {
        var claims = new[] {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.UserName!),
            new Claim(ClaimTypes.Email, user.Email!)
        };
        if (user.Roles != null)
        {
            claims = claims.Append(new Claim(ClaimTypes.Role, string.Join(",", user.Roles))).ToArray();
        }
        return claims;
    }

    private byte[] GetSecretKey() => Encoding.UTF8.GetBytes(_configuration["Jwt:Secret"]!);

    private SigningCredentials CreateSigningCredentials() =>
        new SigningCredentials(
            new SymmetricSecurityKey(
                GetSecretKey()
            ),
            SecurityAlgorithms.HmacSha256Signature
        );

    public async Task RemoveUserData(string username)
    {
        await _redisRepository.Remove(username);
    }
}