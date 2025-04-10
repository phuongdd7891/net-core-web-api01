using CoreLibrary.Const;
using Gateway.Models.Requests;
using Microsoft.AspNetCore.Mvc;
using Adminuserservice;
using Adminauthservice;
using Microsoft.AspNetCore.Authorization;
using Google.Protobuf.WellKnownTypes;
using CoreLibrary.Utils;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Gateway.Controllers;

[ApiController]
[Route("gw-api/[controller]")]
[Authorize]
public class AdminController : BaseController
{
    private readonly AdminUserServiceProto.AdminUserServiceProtoClient _adminUserClient;
    private readonly AdminAuthServiceProto.AdminAuthServiceProtoClient _adminAuthClient;
    private readonly IConfiguration _configuration;

    public AdminController(
        AdminUserServiceProto.AdminUserServiceProtoClient adminUserClient,
        AdminAuthServiceProto.AdminAuthServiceProtoClient adminAuthClient,
        IConfiguration configuration
    )
    {
        _adminUserClient = adminUserClient;
        _adminAuthClient = adminAuthClient;
        _configuration = configuration;
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
            Password = request.Password
        });
        var createIfNotExists = _configuration.GetValue("CreateUserIfNotExists", false);
        if (result.ErrorCode == Const.ErrCode_UserNotFound && createIfNotExists)
        {
            var createResult = await _adminUserClient.CreateUserAsync(new CreateUserRequest
            {
                Username = request.UserName,
                Password = request.Password,
                FullName = request.UserName,
                Email = request.UserName,
                IsSystem = true,
                IsCustomer = false,
                Disabled = false
            }, GetSpecialHeader(request.UserName));
            if (string.IsNullOrEmpty(createResult.Message))
            {
                result = await _adminAuthClient.LoginAsync(new AdminLoginRequest
                {
                    Username = request.UserName,
                    Password = request.Password
                });
            }
            else
            {
                ErrorStatuses.ThrowBadRequest(createResult.Message, true);
            }
        }
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

    [HttpPost("Logout")]
    public IActionResult Logout()
    {
        return Ok(new DataResponse());
    }

    [HttpGet("user-profile")]
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
    public async Task<IActionResult> GetCustomerUsers()
    {
        var result = await _adminUserClient.GetCustomerUsersAsync(new Empty(), DefaultHeader);
        return Ok(new DataResponse<dynamic>
        {
            Data = result.Data.Select(a =>
            {
                return new
                {
                    a.Id,
                    a.Username,
                    a.UserCount,
                    a.FullName,
                    a.Email,
                    a.Disabled,
                    CreatedDate = a.CreatedDate.ToDateTime()
                };
            })
        });
    }

    private void ValidatePasswordRequest(string password)
    {
        ErrorStatuses.ThrowBadRequest("Min length of password is 8", password.Length < 8);
        ErrorStatuses.ThrowBadRequest("Password should have at least a number", !password.Any(c => Char.IsNumber(c)));
        ErrorStatuses.ThrowBadRequest("Password should have at least an upper character", !password.Any(c => !Char.IsNumber(c) && Char.IsUpper(c)));
    }

    [HttpPost("create-user")]
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
        var result = await _adminUserClient.CreateUserAsync(new CreateUserRequest
        {
            Username = request.Username,
            Password = request.Password,
            FullName = request.FullName,
            Email = request.Email,
            IsSystem = request.IsSystem,
            IsCustomer = request.IsCustomer,
            Disabled = request.Disabled
        }, DefaultHeader);
        ErrorStatuses.ThrowBadRequest(result.Message, !string.IsNullOrEmpty(result.Message));
        return Ok(new DataResponse());
    }

    [HttpPost("update-user")]
    public async Task<IActionResult> UpdateUser(AdminUserRequest request)
    {
        if (!string.IsNullOrEmpty(request.Password))
        {
            ValidatePasswordRequest(request.Password);
        }
        else
        {
            request.Password = "pwd";
            ModelState.Remove("Password");
        }
        if (!string.IsNullOrEmpty(request.Email))
        {
            ErrorStatuses.ThrowBadRequest("Invalid email", !Utils.ValidEmailAddress(request.Email));
        }
        else
        {
            request.Email = "email";
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
        var result = await _adminUserClient.UpdateUserAsync(new Adminuserservice.UpdateUserRequest
        {
            Username = request.Username,
            Password = request.Password,
            FullName = request.FullName,
            Email = request.Email,
            Disabled = request.Disabled            
        }, DefaultHeader);
        ErrorStatuses.ThrowBadRequest(result.Message, !string.IsNullOrEmpty(result.Message));
        return Ok(new DataResponse());
    }

    [HttpGet("get-user")]
    public async Task<IActionResult> GetUser(string username)
    {
        ErrorStatuses.ThrowBadRequest("Username is required", string.IsNullOrEmpty(username));
        var user = await _adminUserClient.GetUserAsync(new GetUserProfileRequest
        {
            Username = username
        }, DefaultHeader);
        ErrorStatuses.ThrowNotFound("User not found", user == null);
        return Ok(new DataResponse<AdminUser>
        {
            Data = user
        });
    }
}