using WebApi.Services;
using IdentityMongo.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using WebApi.Models.Requests;

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
    public async Task<ActionResult<DataResponse<string>>> CreateUser(User user)
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
                Email = user.Email
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
        var emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(appUser!);
        //await _emailSender.SendEmailAsync(user.Email, "Email Confirmation Token", $"<p>Please use below token to confirm your account before login</p><p><b>{emailToken}</b></p>").ConfigureAwait(false);
        Console.WriteLine($"{user.Username}: {emailToken}");
        user.Password = "";
        return Ok(new DataResponse());
    }

    [HttpPost("Login")]
    public async Task<ActionResult<DataResponse<AuthenticationResponse>>> Login(AuthenticationRequest request, [FromQuery(Name = "t")] string? tokenType)
    {
        ErrorStatuses.ThrowBadRequest("Username is required", string.IsNullOrEmpty(request.UserName));
        ErrorStatuses.ThrowBadRequest("Password is required", string.IsNullOrEmpty(request.Password));
        var user = await _userManager.FindByNameAsync(request.UserName);
        ErrorStatuses.ThrowNotFound("User not found", user == null);

        var isPasswordValid = await _userManager.CheckPasswordAsync(user!, request.Password);
        ErrorStatuses.ThrowBadRequest("Bad credentials", !isPasswordValid);

        var isConfirmedEmail = await _userManager.IsEmailConfirmedAsync(user!);
        ErrorStatuses.ThrowInternalErr("Email has been not confirmed yet", !isConfirmedEmail, DataResponseCode.EmailNotConfirm.ToString());

        if (tokenType == "jwt")
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
    [Authorize(AuthenticationSchemes = $"{JwtBearerDefaults.AuthenticationScheme}")]
    public async Task<ActionResult<DataResponse>> Logout()
    {
        var username = HttpContext.User.Identity!.Name;
        await _apiKeyService.RemoveRedisToken(username!);
        return Ok(new DataResponse());
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
    public async Task<IActionResult> AddUserRoles(UserRolesRequest req)
    {
        var user = await _userManager.FindByNameAsync(req.Username);

        if (user == null)
        {
            return NotFound("User not found");
        }
        var result = await _userManager.AddToRolesAsync(user, req.Roles);
        if (result.Succeeded)
            return Ok("Add user role Successfully");
        else
        {
            return BadRequest(result.Errors);
        }
    }

    #endregion

    [HttpPost("add-role-action")]
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
    [Authorize(AuthenticationSchemes = $"{JwtBearerDefaults.AuthenticationScheme}")]
    public async Task<ActionResult<DataResponse<bool>>> ChangePassword(ChangePasswordRequest request)
    {
        ErrorStatuses.ThrowBadRequest("Invalid request", string.IsNullOrEmpty(request.CurrentPassword) || string.IsNullOrEmpty(request.NewPassword));
        var user = await _userManager.FindByNameAsync(HttpContext.User.Identity!.Name!);
        var result = await _userManager.ChangePasswordAsync(user!, request.CurrentPassword, request.NewPassword);
        ErrorStatuses.ThrowInternalErr(result.Errors.FirstOrDefault()?.Description ?? "Change password failed", !result.Succeeded);
        return Ok(new DataResponse<bool>
        {
            Data = result.Succeeded
        });
    }

    [HttpPost("confirm-email")]
    public async Task<ActionResult<DataResponse<bool>>> ConfirmEmail(ConfirmEmailRequest request)
    {
        ErrorStatuses.ThrowBadRequest("Invalid request", string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Token));
        var user = await _userManager.FindByNameAsync(request.Username!);
        ErrorStatuses.ThrowNotFound("User not found", user == null);
        var result = await _userManager.ConfirmEmailAsync(user!, request.Token!);
        ErrorStatuses.ThrowInternalErr(result.Errors.FirstOrDefault()?.Description ?? "Confirm failed", !result.Succeeded);
        return Ok(new DataResponse<bool>
        {
            Data = result.Succeeded
        });
    }

    [HttpGet("encrypt")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public ActionResult<string> Encrypt(string value)
    {
        var result = string.IsNullOrEmpty(value) ? string.Empty : AESHelpers.Encrypt(value);
        return Content(result);
    }
}