using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using WebApi.Models.Admin;
using WebApi.Models.Requests;
using WebApi.Services;

[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly AdminService _adminService;
    private readonly JwtService _jwtService;

    public AdminController(
        AdminService adminService,
        JwtService jwtService
    )
    {
        _adminService = adminService;
        _jwtService = jwtService;
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
        var token = _jwtService.CreateAdminToken(user!);
        return Ok(new DataResponse<AuthenticationResponse>
        {
            Data = token
        });
    }
}