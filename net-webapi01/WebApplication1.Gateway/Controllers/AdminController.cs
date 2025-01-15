using CoreLibrary.Const;
using Gateway.Models.Requests;
using Microsoft.AspNetCore.Mvc;
using Adminuserservice;
using Adminauthservice;
using Userservice;
using Microsoft.AspNetCore.Authorization;

namespace Gateway.Controllers;

[ApiController]
[Route("gw-api/[controller]")]
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

    [HttpGet("users")]
    [Authorize]
    public async Task<IActionResult> GetUsers(int skip, int limit, string customerId)
    {
        var listUsers = await _adminUserClient.ListUsersAsync(new ListUsersRequest
        {
            CustomerId = customerId,
            Skip = skip,
            Limit = limit
        }, DefaultHeader);
        return Ok(new DataResponse<dynamic>
        {
            Data = new {
                listUsers.List,
                listUsers.Total
            }
        });
    }

    [HttpGet("get-user")]
    [Authorize]
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
        }, DefaultHeader);
        return Ok(new DataResponse<AdminProfile>
        {
            Data = result.Data
        });
    }
}