using WebApi.Services;
using WebApi.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using WebApi.Models.Requests;
using CoreLibrary.Helpers;
using WebApplication1.Authentication;
using System.Security.Claims;
using Newtonsoft.Json;
using WebApi.Models.Admin;
using CoreLibrary.Utils;
using Microsoft.AspNetCore.Mvc.Controllers;
using System.Reflection;
using System.ComponentModel;

namespace WebApi.Controllers;

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
    private readonly IEnumerable<EndpointDataSource> _endpointSources;

    private readonly AdminService _adminService;

    public OperationsController(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        JwtService jwtService,
        ApiKeyService apiKeyService,
        CacheService cacheSrevice,
        RoleActionRepository roleActionRepository,
        IEmailSender emailSender,
        AdminService adminService,
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

    #region Roles
    [HttpPost("create-role")]
    public async Task<IActionResult> CreateRole([FromBody] ApplicationRoleRequest request)
    {
        IdentityResult result = await _roleManager.CreateAsync(new ApplicationRole() { Name = request.Name, NormalizedName = request.DisplayName });
        ErrorStatuses.ThrowInternalErr(result.Errors.FirstOrDefault()?.Description ?? "", !result.Succeeded);
        await _cacheService.LoadUserRoles();
        return Ok();
    }

    [HttpPost("edit-role")]
    public async Task<IActionResult> EditRole([FromBody] ApplicationRoleRequest request)
    {
        ErrorStatuses.ThrowInternalErr("Invalid request", request == null || string.IsNullOrEmpty(request.Id));
        var role = await _roleManager.FindByIdAsync(request!.Id!);
        ErrorStatuses.ThrowInternalErr("Invalid role", role == null);
        role!.Name = request.Name;
        role.NormalizedName = request.DisplayName;
        await _roleManager.UpdateAsync(role);
        return Ok();
    }

    [HttpPost("add-user-roles")]
    [AdminAuthorize(true, true)]
    public async Task<IActionResult> AddUserRoles(UserRolesRequest req)
    {
        var user = await _userManager.FindByNameAsync(req.Username);
        ErrorStatuses.ThrowNotFound("User not found", user == null);
        if (user!.Roles.Count > 0)
        {
            var roles = _roleManager.Roles.Where(a => user.Roles.Contains(a.Id)).Select(a => a.Name).ToList();
            var result = await _userManager.RemoveFromRolesAsync(user, roles!);
            ErrorStatuses.ThrowInternalErr(result.Errors.FirstOrDefault()?.Description ?? "Remove role fail", !result.Succeeded);
        }
        if (req.Roles.Length > 0)
        {
            var addResult = await _userManager.AddToRolesAsync(user!, req.Roles);
            ErrorStatuses.ThrowInternalErr(addResult.Errors.FirstOrDefault()?.Description ?? "Add role fail", !addResult.Succeeded);
        }
        return Ok();
    }

    [HttpGet("user-roles")]
    [AdminAuthorize(true, true)]
    public async Task<DataResponse<List<GetRolesReply>>> GetUserRoles()
    {
        var dict = new Dictionary<string, List<string>>();
        var roleActions = await _roleActionRepository.GetAll();
        roleActions.ForEach(a =>
        {
            a.Roles.ForEach(x =>
            {
                if (!dict.ContainsKey(x))
                {
                    dict[x] = new List<string>();
                }
                dict[x].Add(a.RequestAction);
            });
        });
        var roles = new List<GetRolesReply>();
        _roleManager.Roles.ToList().ForEach(a =>
        {
            var key = a.Id.ToString();
            roles.Add(new GetRolesReply
            {
                Id = a.Id,
                Name = a.Name,
                DisplayName = a.NormalizedName,
                Actions = dict.ContainsKey(key) ? dict[key] : new List<string>()
            });
        });
        return new DataResponse<List<GetRolesReply>>
        {
            Data = roles
        };
    }

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

    [HttpPost("add-roles-actions")]
    [AdminAuthorize(true)]
    public async Task<IActionResult> AddRoleActions(RoleActionRequest request)
    {
        var appRoles = GetRolesByNames(request.Roles);
        await _roleActionRepository.Add(request.Actions, appRoles.Select(a => Convert.ToString(a.Id)).ToArray()!);
        await _cacheService.LoadRoleActions();
        return Ok();
    }

    [HttpPost("delete-action")]
    [AdminAuthorize(true)]
    public async Task<IActionResult> DeleteRoleAction(string action)
    {
        var result = await _roleActionRepository.Delete(action);
        return Ok(new DataResponse<bool>
        {
            Data = result.DeletedCount > 0
        });
    }

    [HttpGet("request-actions")]
    [AdminAuthorize(true)]
    public async Task<IActionResult> GetRoleActions()
    {
        var actions = await _roleActionRepository.GetAll();
        return Ok(new DataResponse<List<string>>
        {
            Data = actions.Select(a => a.RequestAction).ToList()
        });
    }

    [HttpGet("user-actions")]
    [AdminAuthorize(true)]
    [ApiExplorerSettings(IgnoreApi = true)]
    public IActionResult GetUserActions()
    {
        var endpoints = _endpointSources.SelectMany(es => es.Endpoints);
        var result = endpoints.Where(e => !string.IsNullOrEmpty(string.Format("{0}", e.Metadata.GetMetadata<UserAuthorizeAttribute>()))).Select(
            e =>
            {
                var controller = e.Metadata
                    .OfType<ControllerActionDescriptor>()
                    .FirstOrDefault();
                var action = controller != null
                    ? $"{controller.ControllerName}.{controller.ActionName}"
                    : null;
                var controllerMethod = controller != null
                    ? $"{controller.ControllerTypeInfo.FullName}:{controller.MethodInfo.Name}"
                    : null;
                return new UserActionResponse
                {
                    Method = e.Metadata.OfType<HttpMethodMetadata>().FirstOrDefault()?.HttpMethods?[0],
                    Action = action,
                    ControllerMethod = controllerMethod,
                    DisplayName = controller?.MethodInfo.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? e.DisplayName
                };
            }).ToList();
        return Ok(result);
    }
    #endregion

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

    #region User
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
        ErrorStatuses.ThrowBadRequest("Invalid email", !Utils.ValidEmailAddress(user.Email));
        ErrorStatuses.ThrowBadRequest("Invalid phone", !string.IsNullOrEmpty(user.PhoneNumber) && !Utils.ValidPhoneNumber(user.PhoneNumber));
        var result = await _userManager.CreateAsync(
            new ApplicationUser()
            {
                UserName = user.Username,
                Email = user.Email,
                CustomerId = user.CustomerId
            },
            user.Password!
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

    [HttpPost("lock-user")]
    [AdminAuthorize(true)]
    public async Task<IActionResult> LockUser([FromBody] LockUserRequest request)
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
    [AdminAuthorize(true, true)]
    public async Task<DataResponse<GetUsersReply>> GetUsers(int skip = 0, int limit = 100, string? customerId = null)
    {
        var claimData = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.UserData)!.Value;
        var userData = JsonConvert.DeserializeObject<AdminUser>(claimData);
        var adminUsers = await _adminService.ListUsers(userData!.IsCustomer);
        var qUsers = _userManager.Users.Where(u => string.IsNullOrEmpty(customerId) ? ((u.CustomerId == userData.Id && userData.IsCustomer) || userData.IsSystem) : (u.CustomerId == customerId));
        var appUsers = qUsers.Skip(skip).Take(limit).ToList();
        var total = qUsers.Count();
        var users = appUsers
            .GroupJoin(adminUsers, u => u.CustomerId, a => a.Id, (u, a) => new { Admins = a, User = u })
            .SelectMany(a => a.Admins.DefaultIfEmpty(), (u, a) => new UserViewModel(u.User)
            {
                CustomerName = a?.FullName ?? string.Empty
            }).ToList();
        var tasks = new List<Task>();
        users.ForEach(u =>
        {
            tasks.Add(GetRolesByUser(u.UserName!).ContinueWith(x =>
            {
                u.Roles = x.Result;
            }));
        });
        await Task.WhenAll(tasks);
        return new DataResponse<GetUsersReply>
        {
            Data = new GetUsersReply
            {
                List = users,
                Total = total
            }
        };
    }

    private async Task<string[]> GetRolesByUser(string username)
    {
        var appUser = await _userManager.FindByNameAsync(username);
        var roles = await _userManager.GetRolesAsync(appUser!);
        return roles.ToArray();
    }

    [HttpPost("update-user")]
    [Authorize]
    public async Task<IActionResult> UpdateUser(User user)
    {
        var appUser = await _userManager.FindByNameAsync(user.Username);
        ErrorStatuses.ThrowNotFound("User not found", appUser == null);
        ErrorStatuses.ThrowBadRequest("Invalid email", !Utils.ValidEmailAddress(user.Email));
        ErrorStatuses.ThrowBadRequest("Invalid phone", !string.IsNullOrEmpty(user.PhoneNumber) && !Utils.ValidPhoneNumber(user.PhoneNumber));
        if (!string.IsNullOrEmpty(user.Password))
        {
            appUser!.PasswordHash = _userManager.PasswordHasher.HashPassword(appUser, user.Password);
        }
        appUser!.Email = user.Email;
        appUser.PhoneNumber = user.PhoneNumber;
        appUser.CustomerId = user.CustomerId;
        var result = await _userManager.UpdateAsync(appUser);
        if (!result.Succeeded)
        {
            return BadRequest(new DataResponse<string>
            {
                Code = DataResponseCode.IternalError.ToString(),
                Data = result.Errors.First()!.Description
            });
        }
        return Ok(new DataResponse());
    }

    [HttpGet("user"), AdminAuthorize(true, true)]
    public async Task<IActionResult> GetUser(string username)
    {
        ErrorStatuses.ThrowBadRequest("Invalid request", string.IsNullOrEmpty(username));
        var user = await _userManager.FindByNameAsync(username);
        ErrorStatuses.ThrowNotFound("User not found", user == null);
        var roles = await _userManager.GetRolesAsync(user!);
        AdminUser? customer = null;
        if (!string.IsNullOrEmpty(user!.CustomerId))
        {
            customer = await _adminService.GetUserById(user.CustomerId);
        }
        return Ok(new DataResponse<UserViewModel>
        {
            Data = new UserViewModel(user)
            {
                Roles = roles.ToArray(),
                CustomerName = customer?.FullName ?? string.Empty
            }
        });
    }
    #endregion
}