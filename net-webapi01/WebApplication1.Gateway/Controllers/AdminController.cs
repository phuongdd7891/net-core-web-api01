using CoreLibrary.Const;
using Gateway.Models.Requests;
using Microsoft.AspNetCore.Mvc;
using Adminuserservice;
using Adminauthservice;
using Userservice;
using Microsoft.AspNetCore.Authorization;

namespace Gateway.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AdminController : BaseController
{
    private readonly UserServiceProto.UserServiceProtoClient _userClient;
    private readonly AdminUserServiceProto.AdminUserServiceProtoClient _adminUserClient;
    private readonly AdminAuthServiceProto.AdminAuthServiceProtoClient _adminAuthClient;

    public AdminController(
        UserServiceProto.UserServiceProtoClient userClient,
        AdminUserServiceProto.AdminUserServiceProtoClient adminUserClient,
        AdminAuthServiceProto.AdminAuthServiceProtoClient adminAuthClient
    )
    {
        _userClient = userClient;
        _adminUserClient = adminUserClient;
        _adminAuthClient = adminAuthClient;
    }

    [HttpGet("customer-users")]
    public async Task<IActionResult> GetCustomerUsers()
    {
        var users = new List<AdminUser>();
        var listUsers = await _adminUserClient.ListUsersAsync(new ListUsersRequest
        {
            IsCustomer = true
        });
        users.AddRange(listUsers.List);
        // var claimUser = GetUserClaim();
        // if (claimUser != null)
        // {
        //     if (claimUser.IsSystem)
        //     {
        //         var listUsers = await _adminService.ListUsers(true);
        //         users.AddRange(listUsers);
        //     }
        //     if (claimUser.IsCustomer)
        //     {
        //         users.Add(await _adminService.GetUser(claimUser.Username));
        //     }
        // }
        var appUsers = await _userClient.GetUsersAsync(new Google.Protobuf.WellKnownTypes.Empty());
        var list = users.GroupJoin(appUsers.List, u => u.Id, a => a.CustomerId, (u, a) => new { Admin = u, UserCount = a.Count() }).ToList();
        return Ok(new DataResponse<dynamic>
        {
            Data = list.Select(x => new
            {
                x.Admin.Id,
                x.Admin.Username,
                x.Admin.FullName,
                x.Admin.IsCustomer,
                x.Admin.IsSystem,
                x.Admin.Email,
                x.Admin.Disabled,
                x.Admin.CreatedDate,
                x.UserCount,
            }).ToList()
        });
    }

    [HttpGet("get-user")]
    public async Task<IActionResult> GetUser(string username)
    {
        ErrorStatuses.ThrowBadRequest("Username is required", string.IsNullOrEmpty(username));
        var user = await _adminUserClient.GetUserAsync(new GetUserRequest
        {
            Username = username,
        }, DefaultHeader);
        ErrorStatuses.ThrowNotFound("User not found", user == null || user?.Data == null);
        var userData = user!.Data;
        return Ok(new DataResponse<dynamic>
        {
            Data = new
            {
                userData!.Id,
                userData.Username,
                userData.FullName,
                userData.IsSystem,
                userData.IsCustomer,
                userData.Email,
                CreatedDate = userData.CreatedDate.ToDateTime(),
                ModifiedDate = userData.ModifiedDate.ToDateTime(),
                userData.Disabled
            }
        });
    }

    [HttpPost("Login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(AuthenticationRequest request)
    {
        ErrorStatuses.ThrowBadRequest("Username is required", string.IsNullOrEmpty(request.UserName));
        ErrorStatuses.ThrowBadRequest("Password is required", string.IsNullOrEmpty(request.Password));
        var result = await _adminAuthClient.LoginAsync(new AdminLoginRequest
        {
            Username = request.UserName,
            Password = request.Password,
        });
        ErrorStatuses.ThrowBadRequest("Bad credentials", result.ErrorCode == Const.ErroCode_BadCredential);
        ErrorStatuses.ThrowNotFound("User not found", result.ErrorCode == Const.ErrCode_UserNotFound);
        ErrorStatuses.ThrowInternalErr("Invalid user", result.ErrorCode == Const.ErrCode_InvalidUser);
        ErrorStatuses.ThrowInternalErr("Account is disabled", result.ErrorCode == Const.ErrCode_DisabledAccount);
        return Ok(new DataResponse<dynamic>
        {
            Data = new
            {
                result.Username,
                result.Token,
                ExpiryTime = result.Expiration.ToDateTime(),
                result.RefreshToken
            }
        });
    }

    [HttpGet("user-profile")]
    [Authorize]
    public async Task<IActionResult> GetUserProfile([FromQuery(Name = "u")] string username)
    {
        var result = await _adminUserClient.GetUserProfileAsync(new GetUserProfileRequest
        {
            Username = username
        });
        return Ok(new DataResponse<AdminProfile>
        {
            Data = result.Data
        });
    }
}