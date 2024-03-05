using WebApi.Services;
using WebApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using WebApi.Models.Requests;
using CoreLibrary.Helpers;
using WebApplication1.Authentication;
using WebApi.Controllers;
using System.Runtime.InteropServices;

[ApiController]
[Route("api/[controller]")]
public class OperationsController : ControllerBase
{
    private UserManager<ApplicationUser> _userManager;
    private RoleManager<ApplicationRole> _roleManager;
    private readonly JwtService _jwtService;
    private readonly ApiKeyService _apiKeyService;
    private readonly CacheService _cacheService;
    private readonly RoleActionRepository _roleActionRepository;
    private readonly IEmailSender _emailSender;

    public OperationsController(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        JwtService jwtService,
        ApiKeyService apiKeyService,
        CacheService cacheSrevice,
        RoleActionRepository roleActionRepository,
        IEmailSender emailSender
    )
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _jwtService = jwtService;
        _apiKeyService = apiKeyService;
        _cacheService = cacheSrevice;
        _roleActionRepository = roleActionRepository;
        _emailSender = emailSender;
    }

    [HttpPost("create-user")]
    public async Task<IActionResult> CreateUser(User user)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new DataResponse<string>
            {
                Code = DataResponseCode.InvalidRequest.ToString(),
                Data = ModelState.Values.First().Errors.First()!.ErrorMessage
            });
        }
        var result = await _userManager.CreateAsync(
            new ApplicationUser()
            {
                UserName = user.Username,
                Email = user.Email,
                CustomerId = user.CustomerId
            },
            user.Password
        );
        if (!result.Succeeded)
        {
            return BadRequest(new DataResponse<string>
            {
                Code = DataResponseCode.InvalidRequest.ToString(),
                Data = result.Errors.First()!.Description
            });
        }
        var appUser = await _userManager.FindByNameAsync(user.Username);
        try
        {
            var emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(appUser!);
            //await _emailSender.SendEmailAsync(user.Email, "Email Confirmation Token", $"<p>You need to confirm your email account by using below token</p><p><b>{emailToken}</b></p>").ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await _userManager.DeleteAsync(appUser!);
            return BadRequest(new DataResponse<string>
            {
                Code = DataResponseCode.IternalError.ToString(),
                Data = ex.Message
            });
        }
        return Ok(new DataResponse());
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

    #region Roles
    [HttpPost("create-role")]
    public async Task<IActionResult> CreateRole([FromBody] string name)
    {
        IdentityResult result = await _roleManager.CreateAsync(new ApplicationRole() { Name = name });
        ErrorStatuses.ThrowInternalErr(result.Errors.FirstOrDefault()?.Description ?? "", !result.Succeeded);
        await _cacheService.LoadUserRoles();
        return Ok("Role Created Successfully");
    }

    [HttpPost("add-user-roles")]
    [CustomAuthorize(null, true, true)]
    public async Task<IActionResult> AddUserRoles(UserRolesRequest req)
    {
        var user = await _userManager.FindByNameAsync(req.Username);
        ErrorStatuses.ThrowNotFound("User not found", user == null);
        var result = await _userManager.AddToRolesAsync(user!, req.Roles);
        ErrorStatuses.ThrowInternalErr(result.Errors.First().Description, !result.Succeeded);
        return Ok();
    }

    #endregion

    [HttpPost("add-role-action")]
    [CustomAuthorize(null, true)]
    public async Task<IActionResult> AddRoleAction(RoleActionRequest request)
    {
        var appRole = await _roleManager.FindByNameAsync(request.Role);
        if (appRole == null)
        {
            return NotFound("Role not found");
        }
        await _roleActionRepository.Add(request.Action, appRole!.Id.ToString());
        await _cacheService.LoadRoleActions();
        return Ok();
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

    [HttpPost("lock-user")]
    [CustomAuthorize(null, true)]
    public async Task<IActionResult> LockUser([FromBody]LockUserRequest request)
    {
        ErrorStatuses.ThrowBadRequest("Username is required", string.IsNullOrEmpty(request.Username));
        var user = await _userManager.FindByNameAsync(request.Username!);
        ErrorStatuses.ThrowNotFound("User not found", user == null);
        if (request.IsLock)
        {
            var result = await _userManager.SetLockoutEndDateAsync(user!, DateTimeOffset.MaxValue);
            ErrorStatuses.ThrowInternalErr(result.Errors.FirstOrDefault()?.Description ?? "Lock failed", !result.Succeeded);
        }
        else
        {
            var result = await _userManager.SetLockoutEndDateAsync(user!, DateTimeOffset.UtcNow.AddDays(-1));
            ErrorStatuses.ThrowInternalErr(result.Errors.FirstOrDefault()?.Description ?? "Unlock failed", !result.Succeeded);
        }
        return Ok();
    }

    [HttpGet("users")]
    [CustomAuthorize(null, true, true)]
    public async Task<DataResponse<GetUsersReply>> GetUsers(int skip = 0, int limit = 100)
    {
        var users = _userManager.Users.Select(c => new UserViewModel
        {
            Id = c.Id,
            UserName = c.UserName,
            Email = c.Email
        }).Skip(skip).Take(limit).ToList();
        var tasks = new List<Task>();
        users.ForEach(u =>
        {
            tasks.Add(GetRolesByUser(u));
        });
        await Task.WhenAll(tasks);
        return new DataResponse<GetUsersReply>
        {
            Data = new GetUsersReply
            {
                List = users,
                Total = _userManager.Users.Count()
            }
        };
    }

    private async Task GetRolesByUser(UserViewModel user)
    {
        var appUser = await _userManager.FindByNameAsync(user.UserName!);
        var roles = await _userManager.GetRolesAsync(appUser!);
        user.Roles = roles.ToArray();
        user.IsLocked = appUser!.LockoutEnd.HasValue && DateTimeOffset.Compare(appUser!.LockoutEnd.Value, DateTimeOffset.UtcNow) > 0;
    }
}