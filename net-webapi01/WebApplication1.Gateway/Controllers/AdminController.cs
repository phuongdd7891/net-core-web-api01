using CoreLibrary.Const;
using Gateway.Models.Requests;
using Microsoft.AspNetCore.Mvc;
using Adminuserservice;
using Adminauthservice;
using Userservice;
using Microsoft.AspNetCore.Authorization;
using Google.Protobuf.WellKnownTypes;

namespace Gateway.Controllers;

[ApiController]
[Route("gw-api/[controller]")]
public class AdminController : BaseController
{
    private readonly AdminUserServiceProto.AdminUserServiceProtoClient _adminUserClient;
    private readonly AdminAuthServiceProto.AdminAuthServiceProtoClient _adminAuthClient;

    public AdminController(
        AdminUserServiceProto.AdminUserServiceProtoClient adminUserClient,
        AdminAuthServiceProto.AdminAuthServiceProtoClient adminAuthClient
    )
    {
        _adminUserClient = adminUserClient;
        _adminAuthClient = adminAuthClient;
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

    [HttpGet("customer-users")]
    [Authorize]
    public async Task<IActionResult> GetCustomerUsers()
    {
        var result = await _adminUserClient.GetCustomerUsersAsync(new Empty(), DefaultHeader);
        return Ok(new DataResponse<dynamic>
        {
            Data = result.Data.Select(a => {
                return new {
                    a.Id,
                    a.Username,
                    a.UserCount,
                    a.FullName,
                    a.Email,
                    CreatedDate = a.CreatedDate.ToDateTime()
                };
            })
        });
    }
}