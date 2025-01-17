using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Userservice;

namespace Gateway.Controllers;

[ApiController]
[Route("gw-api/[controller]")]
public class UserController : BaseController
{
    private readonly UserServiceProto.UserServiceProtoClient _userClient;
    public UserController(
        UserServiceProto.UserServiceProtoClient userClient
    )
    {
        _userClient = userClient;
    }

    [HttpGet("list")]
    [Authorize]
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
            Data = new {
                listUsers.List,
                listUsers.Total
            }
        });
    }

    [HttpGet("get")]
    [Authorize]
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
    [Authorize]
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
}