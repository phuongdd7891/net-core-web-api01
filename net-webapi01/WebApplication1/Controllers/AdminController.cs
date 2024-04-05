using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApi.Models.Admin;
using WebApi.Models.Requests;
using WebApi.Services;
using WebApplication1.Authentication;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly AdminService _adminService;
    private readonly JwtService _jwtService;
    private readonly ApiKeyService _apiKeyService;

    public AdminController(
        AdminService adminService,
        JwtService jwtService,
        ApiKeyService apiKeyService
    )
    {
        _adminService = adminService;
        _jwtService = jwtService;
        _apiKeyService = apiKeyService;
    }

    [HttpPost("create-user")]
    public async Task<IActionResult> CreateUser(AdminUserRequest request)
    {
        ErrorStatuses.ThrowBadRequest("Username is required", string.IsNullOrEmpty(request.Username));
        ErrorStatuses.ThrowBadRequest("Password is required", string.IsNullOrEmpty(request.Password));
        ErrorStatuses.ThrowBadRequest("Min length of password is 8", request.Password.Length < 8);
        ErrorStatuses.ThrowBadRequest("Password should have at least a number", !request.Password.Any(c => Char.IsNumber(c)));
        ErrorStatuses.ThrowBadRequest("Password should have at least an upper character", !request.Password.Any(c => !Char.IsNumber(c) && Char.IsUpper(c)));
        var user = await _adminService.GetUser(request.Username);
        ErrorStatuses.ThrowBadRequest("Username is duplicated", user != null);
        await _adminService.CreateUser(new AdminUser
        {
            Username = request.Username,
            Password = request.Password,
            FullName = request.FullName,
            IsSystem = request.IsSystem,
            IsCustomer = request.IsCustomer,
            CreatedDate = DateTime.Now
        });
        return Ok(new DataResponse());
    }

    [HttpPost("Login")]
    public async Task<IActionResult> Login(AuthenticationRequest request)
    {
        ErrorStatuses.ThrowBadRequest("Username is required", string.IsNullOrEmpty(request.UserName));
        ErrorStatuses.ThrowBadRequest("Password is required", string.IsNullOrEmpty(request.Password));
        var user = await _adminService.GetUser(request.UserName);
        ErrorStatuses.ThrowNotFound("User not found", user == null);
        var pwdResult = await _adminService.VerifyPassword(request.UserName, request.Password);
        ErrorStatuses.ThrowBadRequest("Bad credentials", !pwdResult);
        ErrorStatuses.ThrowInternalErr("Invalid user", !user!.IsSystem && !user.IsCustomer);

        var refreshToken = _jwtService.GenerateRefreshToken();
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryDate = DateTime.UtcNow.AddDays(7);
        await _adminService.UpdateUser(user);
        var token = await _jwtService.CreateAdminToken(user!);
        token.RefreshToken = refreshToken;
        return Ok(new DataResponse<AuthenticationResponse>
        {
            Data = token
        });
    }

    [HttpPost("Logout")]
    [Authorize]
    public async Task<DataResponse> Logout()
    {
        var username = HttpContext.User.Identity!.Name;
        await _apiKeyService.RemoveRedisToken(username!);
        return new DataResponse();
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken(AdminRefreshTokenRequest request)
    {
        ErrorStatuses.ThrowBadRequest("Invalid request", string.IsNullOrEmpty(request.RefreshToken) || string.IsNullOrEmpty(request.AccessToken));
        var claims = await _jwtService.GetClaimsFromToken(request.AccessToken);
        ErrorStatuses.ThrowBadRequest("Invalid token", claims == null);
        var username = claims!.FindFirst(c => c.Type == ClaimTypes.Name)?.Value ?? string.Empty;
        var user = await _adminService.GetUser(username);
        ErrorStatuses.ThrowBadRequest("Invalid refresh token", String.Compare(user.RefreshToken, request.RefreshToken) != 0);
        ErrorStatuses.ThrowBadRequest("Refresh token expired", !user.RefreshTokenExpiryDate.HasValue || DateTime.Compare(user.RefreshTokenExpiryDate.Value, DateTime.UtcNow) <= 0);

        var refreshToken = _jwtService.GenerateRefreshToken();
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryDate = DateTime.UtcNow.AddDays(7);
        await _adminService.UpdateUser(user);
        var token = await _jwtService.CreateAdminToken(user!, claims.Claims.ToArray());
        token.RefreshToken = refreshToken;
        return Ok(new DataResponse<AuthenticationResponse>
        {
            Data = token
        });
    }

    [HttpPost("revoke-token")]
    [Authorize]
    public async Task<IActionResult> Revoke(string username)
    {
        ErrorStatuses.ThrowBadRequest("Invalid request", string.IsNullOrEmpty(username));
        var user = await _adminService.GetUser(username!);
        ErrorStatuses.ThrowNotFound("User not found", user == null);
        user!.RefreshToken = null;
        user.RefreshTokenExpiryDate = null;
        await _adminService.UpdateUser(user);
        return Ok();
    }

    [HttpGet("user-profile"), CustomAuthorize(null, true, true)]
    public async Task<IActionResult> GetUserProfile()
    {
        var username = User.Identity!.Name;
        var user = await _adminService.GetUser(username!);
        return Ok(new DataResponse<AdminProfile>
        {
            Data = new AdminProfile
            {
                Id = user.Id,
                Username = user.Username,
                IsSystem = user.IsSystem,
                IsCustomer = user.IsCustomer,
                FullName = user.FullName
            }
        });
    }

    [HttpGet("customer-users"), CustomAuthorize(null, true)]
    public async Task<IActionResult> GetCustomerUsers()
    {
        var users = await _adminService.ListUsers(true);
        return Ok(new DataResponse<List<AdminUser>>
        {
            Data = users
        });
    }
}