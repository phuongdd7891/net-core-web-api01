using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Userservice;
using Gateway.Models.Requests;
using CoreLibrary.Utils;
using Adminauthservice;

namespace Gateway.Controllers;

[ApiController]
[Route("gw-api/[controller]")]
[Authorize]
public partial class UserController : BaseController
{
    private readonly UserServiceProto.UserServiceProtoClient _userClient;
    private readonly AdminAuthServiceProto.AdminAuthServiceProtoClient _adminAuthClient;
    private readonly IEnumerable<EndpointDataSource> _endpointDataSources;
    public UserController(
        UserServiceProto.UserServiceProtoClient userClient,
        AdminAuthServiceProto.AdminAuthServiceProtoClient adminAuthClient,
        IEnumerable<EndpointDataSource> endpointDataSources
    )
    {
        _userClient = userClient;
        _adminAuthClient = adminAuthClient;
        _endpointDataSources = endpointDataSources;
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetUsers(int skip, int limit, string customerId)
    {
        var listUsers = await _userClient.ListUsersAsync(new ListUsersRequest
        {
            CustomerId = customerId,
            Skip = skip,
            Limit = limit
        }, DefaultHeader);
        return Ok(new DataResponse<dynamic>
        {
            Data = new
            {
                listUsers.List,
                listUsers.Total
            }
        });
    }

    [HttpGet("get")]
    public async Task<IActionResult> GetUser(string username)
    {
        ErrorStatuses.ThrowBadRequest("Username is required", string.IsNullOrEmpty(username));
        var user = await _userClient.GetUserAsync(new GetUserRequest
        {
            Username = username,
        }, DefaultHeader);
        ErrorStatuses.ThrowNotFound("User not found", user.Data == null);
        return Ok(new DataResponse<dynamic>
        {
            Data = user!.Data
        });
    }

    [HttpGet("user-roles")]
    public async Task<IActionResult> GetUserRoles(string customerId)
    {
        var roles = await _userClient.GetUserRolesAsync(new GetUserRolesRequest
        {
            CustomerId = customerId
        }, DefaultHeader);
        return Ok(new DataResponse<dynamic>
        {
            Data = roles.Data
        });
    }

    [HttpPost("update-user")]
    public async Task<IActionResult> UpdateUser(UserRequest user)
    {
        ErrorStatuses.ThrowBadRequest("Invalid email", !Utils.ValidEmailAddress(user.Email));
        ErrorStatuses.ThrowBadRequest("Invalid phone", !string.IsNullOrEmpty(user.PhoneNumber) && !Utils.ValidPhoneNumber(user.PhoneNumber));
        var request = new UpdateUserRequest
        {
            Username = user.Username,
            CustomerId = user.CustomerId,
            Email = user.Email,
            Password = user.Password,
            PhoneNumber = user.PhoneNumber,
            IsLocked = user.IsLocked
        };
        request.Roles.AddRange(user.Roles);
        var result = await _userClient.UpdateUserAsync(request, DefaultHeader);
        if (!string.IsNullOrEmpty(result.Message))
        {
            return BadRequest(new DataResponse<string>
            {
                Code = DataResponseCode.IternalError.ToString(),
                Data = result.Message
            });
        }
        return Ok(new DataResponse());
    }

    [HttpPost("lock-user")]
    public async Task<IActionResult> LockUser([FromBody] Models.Requests.LockUserRequest request)
    {
        ErrorStatuses.ThrowBadRequest(ModelState.Values.FirstOrDefault()?.Errors.FirstOrDefault()?.ErrorMessage ?? "Invalid request", !ModelState.IsValid);
        var result = await _userClient.LockUserAsync(new Userservice.LockUserRequest
        {
            Username = request.Username,
            IsLock = request.IsLock
        }, DefaultHeader);
        ErrorStatuses.ThrowInternalErr(result.Message, !result.IsSuccess);
        return Ok();
    }

    [HttpPost("create-user")]
    public async Task<IActionResult> CreateUser(UserRequest user)
    {
        ErrorStatuses.ThrowBadRequest(ModelState.Values.FirstOrDefault()?.Errors.FirstOrDefault()?.ErrorMessage ?? "Invalid request", !ModelState.IsValid);
        ErrorStatuses.ThrowBadRequest("Invalid email", !Utils.ValidEmailAddress(user.Email));
        ErrorStatuses.ThrowBadRequest("Invalid phone", !string.IsNullOrEmpty(user.PhoneNumber) && !Utils.ValidPhoneNumber(user.PhoneNumber));
        var request = new UpdateUserRequest
        {
            Username = user.Username,
            CustomerId = user.CustomerId,
            Email = user.Email,
            Password = user.Password,
            PhoneNumber = user.PhoneNumber
        };
        request.Roles.AddRange(user.Roles);
        var result = await _userClient.CreateUserAsync(request, DefaultHeader);
        ErrorStatuses.ThrowBadRequest(result.Message, !string.IsNullOrEmpty(result.Message));

        //TODO: await _emailSender.SendEmailAsync(user.Email, "Email Confirmation Token", $"<p>You need to confirm your email account by using below token</p><p><b>{emailToken}</b></p>").ConfigureAwait(false);
        return Ok(new DataResponse());
    }
}