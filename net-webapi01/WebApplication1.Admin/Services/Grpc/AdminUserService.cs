using System.Security.Claims;
using Adminuserservice;
using CoreLibrary.DataModels;
using CoreLibrary.Models;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Identity;
using Newtonsoft.Json;
using WebApi.Data;
using AdminUser = Adminuserservice.AdminUser;

namespace WebApi.Services.Grpc;
public class AdminUserGrpcService : AdminUserServiceProto.AdminUserServiceProtoBase
{
    private readonly AdminRepository _adminRepository;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminUserGrpcService(
        AdminRepository adminRepository,
        UserManager<ApplicationUser> userManager
    )
    {
        _adminRepository = adminRepository;
        _userManager = userManager;
    }

    public override async Task<ListUsersReply> ListUsers(ListUsersRequest request, ServerCallContext context)
    {
        var result = new ListUsersReply();
        var claimsPrincipal = Helpers.GetClaimsPrincipal(context);
        var userData = JsonConvert.DeserializeObject<Models.AdminProfile>(claimsPrincipal.FindFirst(ClaimTypes.UserData)?.Value ?? string.Empty);
        if (userData == null)
        {
            return result;
        }
        var customerUsers = await _adminRepository.ListUsers(userData.IsCustomer);
        var qUsers = _userManager.Users.Where(u => string.IsNullOrEmpty(request.CustomerId) ? ((u.CustomerId == userData.Id && userData.IsCustomer) || userData.IsSystem) : (u.CustomerId == request.CustomerId));
        var appUsers = qUsers.Skip(request.Skip).Take(request.Limit).ToList();
        var total = qUsers.Count();
        var users = appUsers
            .GroupJoin(customerUsers, u => u.CustomerId, a => a.Id, (u, a) => new { Admins = a, User = u })
            .SelectMany(a => a.Admins.DefaultIfEmpty(), (u, a) => new CoreLibrary.Models.UserViewModel(u.User)
            {
                CustomerName = a?.FullName ?? string.Empty
            }).ToList();
        var tasks = new List<Task>();
        users.ForEach(u =>
        {
            tasks.Add(GetRolesByUser(u.UserName!).ContinueWith(x =>
            {
                u.Roles = x.Result.Select(a => FormatRoleNameByCustomer(a, u.CustomerId)).ToArray();
            }));
        });
        await Task.WhenAll(tasks);
        result.List.AddRange(users.Select(user =>
        {
            var userVM = new Adminuserservice.UserViewModel
            {
                Id = Convert.ToString(user.Id),
                CustomerId = user.CustomerId,
                CustomerName = user.CustomerName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                IsLocked = user.IsLocked,
                UserName = user.UserName
            };
            userVM.Roles.AddRange(user.Roles);
            return userVM;
        }));
        result.Total = total;
        return result;
    }

//     public async Task<IActionResult> GetUser(string username)
//     {
//         ErrorStatuses.ThrowBadRequest("Invalid request", string.IsNullOrEmpty(username));
//         
//         return Ok(new DataResponse<UserViewModel>
//         {
//             Data = new UserViewModel(user)
//             {
//                 Roles = roles.ToArray(),
//                 CustomerName = customer?.FullName ?? string.Empty
//             }
//         });
//     }
// }
    public override async Task<GetUserReply> GetUser(GetUserRequest request, ServerCallContext context)
    {
        var result = new GetUserReply();
        var user = await _adminRepository.GetUser(request.Username);
        if (user != null)
        {
            result.Data = new AdminUser
            {
                Id = user.Id,
                Username = user.Username,
                Disabled = user.Disabled,
                FullName = user.FullName ?? string.Empty,
                CreatedDate = Timestamp.FromDateTime(user.CreatedDate),
                ModifiedDate = user.ModifiedDate.HasValue ? Timestamp.FromDateTime(user.ModifiedDate.Value) : null,
                IsCustomer = user.IsCustomer,
                IsSystem = user.IsSystem,
            };
        }
        return result;
    }

    public override async Task<GetUserProfileReply> GetUserProfile(GetUserProfileRequest request, ServerCallContext context)
    {
        var user = await _adminRepository.GetUser(request.Username);
        return new GetUserProfileReply
        {
            Data = new AdminProfile
            {
                Id = user.Id,
                Username = user.Username,
                IsSystem = user.IsSystem,
                IsCustomer = user.IsCustomer,
                FullName = user.FullName
            }
        };
    }

    private async Task<string[]> GetRolesByUser(string username)
    {
        var appUser = await _userManager.FindByNameAsync(username);
        var roles = await _userManager.GetRolesAsync(appUser!);
        return roles.ToArray();
    }

    private string FormatRoleNameByCustomer(string originalName, string? customerId)
    {
        var roleNameArr = originalName.Split("__", StringSplitOptions.RemoveEmptyEntries);
        return string.IsNullOrEmpty(customerId) ? originalName : string.Join("", roleNameArr, roleNameArr.Length > 1 ? 1 : 0, roleNameArr.Length > 1 ? roleNameArr.Length - 1 : roleNameArr.Length);
    }
}