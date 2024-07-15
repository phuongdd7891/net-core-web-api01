using System.Security.Claims;
using System.Security.Principal;

namespace WebApplication1.Authentication;

public class UserIdentity : GenericIdentity
{
    public string? Username { get; private set; }
    public string[] UserRoles { get; private set; }

    public UserIdentity(Claim[] claims) : base(claims.FirstOrDefault(a => a.Type == ClaimTypes.Name)?.Value ?? string.Empty)
    {
        Username = claims.FirstOrDefault(x => x.Type == ClaimTypes.Name)?.Value;
        UserRoles = claims.FirstOrDefault(x => x.Type == ClaimTypes.Role)?.Value.Split(",") ?? Array.Empty<string>();
    }
}