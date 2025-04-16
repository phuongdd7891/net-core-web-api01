using System.Security.Claims;
namespace Gateway.Authorization;

public interface IUserIdentity
{
    string UserId { get; }
    string Username { get; }
    string[] UserRoles { get; }
}

public class UserIdentity: IUserIdentity
{
    private readonly IHttpContextAccessor _contextAccessor;

    public UserIdentity(IHttpContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor;
    }
    private ClaimsPrincipal User => _contextAccessor.HttpContext?.User ?? new ClaimsPrincipal();
    public string UserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    public string Username => User.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;
    public string[] UserRoles => User.FindFirst(ClaimTypes.Role)?.Value.Split(',') ?? Array.Empty<string>();
}