using WebApi.Services;
using WebApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using WebApi.Models.Requests;
using CoreLibrary.Helpers;
using WebApi.SSE;

namespace WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public partial class OperationsController : BaseController
{
    private UserManager<ApplicationUser> _userManager;
    private RoleManager<ApplicationRole> _roleManager;
    private readonly JwtService _jwtService;
    private readonly ApiKeyService _apiKeyService;
    private readonly CacheService _cacheService;
    private readonly RoleActionRepository _roleActionRepository;
    private readonly IEmailSender _emailSender;
    private readonly IEnumerable<EndpointDataSource> _endpointSources;

    private readonly AdminService _adminService;
    private readonly ISseHolder _sseHolder;

    public OperationsController(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        JwtService jwtService,
        ApiKeyService apiKeyService,
        CacheService cacheSrevice,
        RoleActionRepository roleActionRepository,
        IEmailSender emailSender,
        AdminService adminService,
        ISseHolder sseHolder,
        IEnumerable<EndpointDataSource> endpointSources
    )
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _jwtService = jwtService;
        _apiKeyService = apiKeyService;
        _cacheService = cacheSrevice;
        _roleActionRepository = roleActionRepository;
        _emailSender = emailSender;
        _adminService = adminService;
        _sseHolder = sseHolder;
        _endpointSources = endpointSources;
    }

    [HttpPost("Login")]
    public async Task<IActionResult> Login(AuthenticationRequest request, [FromQuery(Name = "t")] string? tokenType)
    {
        ErrorStatuses.ThrowBadRequest("Username is required", string.IsNullOrEmpty(request.UserName));
        ErrorStatuses.ThrowBadRequest("Password is required", string.IsNullOrEmpty(request.Password));
        var user = await _userManager.FindByNameAsync(request.UserName);
        ErrorStatuses.ThrowNotFound("User not found", user == null);

        var isPasswordValid = await _userManager.CheckPasswordAsync(user!, request.Password);
        ErrorStatuses.ThrowBadRequest("Bad credentials", !isPasswordValid);

        ErrorStatuses.ThrowBadRequest("User is locked out", user!.LockoutEnd.HasValue && DateTimeOffset.Compare(user!.LockoutEnd.Value, DateTimeOffset.UtcNow) > 0);

        var isConfirmedEmail = await _userManager.IsEmailConfirmedAsync(user!);
        ErrorStatuses.ThrowInternalErr("Email has been not confirmed yet", !isConfirmedEmail, DataResponseCode.EmailNotConfirm.ToString());

        if (tokenType == null)
        {
            var token = await _jwtService.CreateToken(user!);
            return Ok(new DataResponse<AuthenticationResponse>
            {
                Data = token
            });
        }
        else
        {
            var token = await _apiKeyService.CreateRedisToken(user!);
            return Ok(new DataResponse<UserApiKey>
            {
                Data = token
            });
        }
    }

    [HttpPost("Logout")]
    [Authorize]
    public async Task<DataResponse> Logout()
    {
        var username = HttpContext.User.Identity!.Name;
        await _apiKeyService.RemoveRedisToken(username!);
        return new DataResponse();
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<DataResponse<bool>> ChangePassword(ChangePasswordRequest request)
    {
        ErrorStatuses.ThrowBadRequest("Invalid request", string.IsNullOrEmpty(request.CurrentPassword) || string.IsNullOrEmpty(request.NewPassword));
        var user = await _userManager.FindByNameAsync(HttpContext.User.Identity!.Name!);
        var result = await _userManager.ChangePasswordAsync(user!, request.CurrentPassword, request.NewPassword);
        ErrorStatuses.ThrowInternalErr(result.Errors.FirstOrDefault()?.Description ?? "Change password failed", !result.Succeeded);
        return new DataResponse<bool>
        {
            Data = result.Succeeded
        };
    }

    [HttpPost("confirm-email")]
    public async Task<DataResponse<bool>> ConfirmEmail(ConfirmEmailRequest request)
    {
        ErrorStatuses.ThrowBadRequest("Invalid request", string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Token));
        var user = await _userManager.FindByNameAsync(request.Username!);
        ErrorStatuses.ThrowNotFound("User not found", user == null);
        var result = await _userManager.ConfirmEmailAsync(user!, request.Token!);
        ErrorStatuses.ThrowInternalErr(result.Errors.FirstOrDefault()?.Description ?? "Confirm failed", !result.Succeeded);
        return new DataResponse<bool>
        {
            Data = result.Succeeded
        };
    }

    [HttpGet("generate-email-token")]
    public async Task<DataResponse<string>> GenerateConfirmationEmailToken(string username)
    {
        ErrorStatuses.ThrowBadRequest("Invalid request", string.IsNullOrEmpty(username));
        var user = await _userManager.FindByNameAsync(username!);
        ErrorStatuses.ThrowNotFound("User not found", user == null);
        var emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(user!);
        return new DataResponse<string>
        {
            Data = emailToken
        };
    }

    [HttpGet("encrypt")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public ActionResult<string> Encrypt(string value)
    {
        var result = string.IsNullOrEmpty(value) ? string.Empty : AESHelpers.Encrypt(value);
        return Content(result);
    }

    #region utilities
    private IEnumerable<ApplicationRole> GetRolesByNames(string[] names)
    {
        foreach (var role in names)
        {
            var appRole = _roleManager.Roles.FirstOrDefault(a => a.Name == role);
            if (appRole != null)
            {
                yield return appRole;
            }
        }
    }
    #endregion

    [HttpPost("/sse/message")]
    [Authorize]
    public async Task<string> SendMessage([FromBody] SseMessage? message)
    {
        if(string.IsNullOrEmpty(message?.Id) ||
            string.IsNullOrEmpty(message?.Message))
        {
            var msg = message?.Message ?? "";
            await _sseHolder.SendMessageAsync(HttpContext.User.Identity!.Name!, msg);
            return msg;
        }
        await _sseHolder.SendMessageAsync(message);
        return "";
    }
}