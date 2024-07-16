using System.Security.Claims;
using CoreLibrary.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebApi.Models;
using WebApi.Models.Admin;
using WebApi.Models.Requests;
using WebApi.Services;
using WebApi.Authentication;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly AdminService _adminService;
    private readonly JwtService _jwtService;
    private readonly ApiKeyService _apiKeyService;
    private UserManager<ApplicationUser> _userManager;

    public AdminController(
        AdminService adminService,
        JwtService jwtService,
        ApiKeyService apiKeyService,
        UserManager<ApplicationUser> userManager
    )
    {
        _adminService = adminService;
        _jwtService = jwtService;
        _apiKeyService = apiKeyService;
        _userManager = userManager;
    }

    private AdminUser? GetUserClaim()
    {
        var userData = User.FindFirstValue(ClaimTypes.UserData);
        if (!string.IsNullOrEmpty(userData))
        {
            return JsonConvert.DeserializeObject<AdminUser>(userData)!;
        }
        return null;
    }

    private void ValidatePasswordRequest(string password)
    {
        ErrorStatuses.ThrowBadRequest("Min length of password is 8", password.Length < 8);
        ErrorStatuses.ThrowBadRequest("Password should have at least a number", !password.Any(c => Char.IsNumber(c)));
        ErrorStatuses.ThrowBadRequest("Password should have at least an upper character", !password.Any(c => !Char.IsNumber(c) && Char.IsUpper(c)));
    }

    [HttpPost("create-user"), AdminAuthorize(true)]
    public async Task<IActionResult> CreateUser(AdminUserRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new DataResponse<string>
            {
                Code = DataResponseCode.InvalidRequest.ToString(),
                Data = ModelState.Values.First().Errors.First()!.ErrorMessage
            });
        }
        ValidatePasswordRequest(request.Password);
        var user = await _adminService.GetUser(request.Username);
        ErrorStatuses.ThrowBadRequest("Username is duplicated", user != null);
        await _adminService.CreateUser(new AdminUser
        {
            Username = request.Username,
            Password = request.Password,
            FullName = request.FullName,
            Email = request.Email,
            IsSystem = request.IsSystem,
            IsCustomer = request.IsCustomer,
            Disabled = request.Disabled,
        });
        return Ok(new DataResponse());
    }

    [HttpPost("update-user"), AdminAuthorize(true)]
    public async Task<IActionResult> EditUser(AdminUserRequest request)
    {
        var user = await _adminService.GetUser(request.Username);
        ErrorStatuses.ThrowNotFound("User not found", user == null);
        if (!string.IsNullOrEmpty(request.Password))
        {
            ValidatePasswordRequest(request.Password);
        }
        else
        {
            request.Password = user!.Password;
            ModelState.Remove("Password");
        }
        if (!string.IsNullOrEmpty(request.Email))
        {
            ErrorStatuses.ThrowBadRequest("Invalid email", !Utils.ValidEmailAddress(request.Email));
        }
        else
        {
            request.Email = user!.Email!;
            ModelState.Remove("Email");
        }
        if (!ModelState.IsValid)
        {
            return BadRequest(new DataResponse<string>
            {
                Code = DataResponseCode.InvalidRequest.ToString(),
                Data = ModelState.Values.First().Errors.First()!.ErrorMessage
            });
        }
        await _adminService.UpdateUser(new AdminUser
        {
            Id = user!.Id,
            Username = request.Username,
            Password = request.Password,
            FullName = request.FullName,
            Email = request.Email,
            Disabled = request.Disabled,
            CreatedDate = user.CreatedDate,
            IsCustomer = true,
        }, request.Password);
        return Ok(new DataResponse());
    }

    [HttpPost("Login")]
    public async Task<IActionResult> Login(AuthenticationRequest request)
    {
        ErrorStatuses.ThrowBadRequest("Username is required", string.IsNullOrEmpty(request.UserName));
        ErrorStatuses.ThrowBadRequest("Password is required", string.IsNullOrEmpty(request.Password));
        var user = await _adminService.GetUser(request.UserName);
        ErrorStatuses.ThrowNotFound("User not found", user == null);
        ErrorStatuses.ThrowInternalErr("Invalid user", !user!.IsSystem && !user.IsCustomer);
        ErrorStatuses.ThrowInternalErr("Account is disabled", user.Disabled);
        var pwdResult = await _adminService.VerifyPassword(request.UserName, request.Password);
        ErrorStatuses.ThrowBadRequest("Bad credentials", !pwdResult);

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

    [HttpGet("user-profile"), AdminAuthorize(true, true)]
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

    [HttpGet("customer-users"), AdminAuthorize(true, true)]
    public async Task<IActionResult> GetCustomerUsers()
    {
        var users = new List<AdminUser>();
        var claimUser = GetUserClaim();
        if (claimUser != null)
        {
            if (claimUser.IsSystem)
            {
                var listUsers = await _adminService.ListUsers(true);
                users.AddRange(listUsers);
            }
            if (claimUser.IsCustomer)
            {
                users.Add(await _adminService.GetUser(claimUser.Username));
            }
        }
        var appUsers = _userManager.Users;
        var list = users.GroupJoin(appUsers, u => u.Id, a => a.CustomerId, (u, a) => new { Admin = u, UserCount = a.Count() }).ToList();
        return Ok(new DataResponse<List<AdminProfile>>
        {
            Data = list.Select(x => new AdminProfile
            {
                Id = x.Admin.Id,
                Username = x.Admin.Username,
                FullName = x.Admin.FullName,
                IsCustomer = x.Admin.IsCustomer,
                IsSystem = x.Admin.IsSystem,
                Email = x.Admin.Email,
                Disabled = x.Admin.Disabled,
                CreatedDate = x.Admin.CreatedDate,
                UserCount = x.UserCount,
            }).ToList()
        });
    }

    [HttpGet("get-user"), AdminAuthorize(true)]
    public async Task<IActionResult> GetUser(string username)
    {
        ErrorStatuses.ThrowBadRequest("Username is required", string.IsNullOrEmpty(username));
        var user = await _adminService.GetUser(username);
        ErrorStatuses.ThrowNotFound("User not found", user == null);
        return Ok(new DataResponse<AdminUser>
        {
            Data = user 
        });
    }

    [HttpPost("change-password"), AdminAuthorize(true, true)]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        ErrorStatuses.ThrowBadRequest("Invalid request", string.IsNullOrEmpty(request.CurrentPassword) || string.IsNullOrEmpty(request.NewPassword));
        var username = User.Identity!.Name!;
        var pwdResult = await _adminService.VerifyPassword(username, request.CurrentPassword);
        ErrorStatuses.ThrowBadRequest("Invalid current password", !pwdResult);
        var newPwdResult = await _adminService.VerifyPassword(username, request.NewPassword);
        ErrorStatuses.ThrowBadRequest("New password have to different with current password", newPwdResult);
        ValidatePasswordRequest(request.NewPassword);
        var user = await _adminService.GetUser(username);
        await _adminService.UpdateUser(user, request.NewPassword);
        return Ok();
    }
}