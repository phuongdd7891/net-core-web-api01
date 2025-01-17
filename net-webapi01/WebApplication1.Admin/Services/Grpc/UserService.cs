using CoreLibrary.DataModels;
using Grpc.Core;
using Microsoft.AspNetCore.Identity;
using Userservice;
using WebApi.Data;
using WebApi.Models;

namespace WebApi.Services.Grpc;
public partial class UserGrpcService : UserServiceProto.UserServiceProtoBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly AdminRepository _adminRepository;
    private readonly RoleActionRepository _roleActionRepository;

    public UserGrpcService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        AdminRepository adminRepository,
        RoleActionRepository roleActionRepository
    )
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _adminRepository = adminRepository;
        _roleActionRepository = roleActionRepository;
    }

    public override async Task<ListUsersReply> ListUsers(ListUsersRequest request, ServerCallContext context)
    {
        var result = new ListUsersReply();
        var userData = Helpers.GetClaimProfile(context);
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
            var userVM = new Userservice.UserViewModel
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

    public override async Task<GetUserReply> GetUser(GetUserRequest request, ServerCallContext context)
    {
        var result = new GetUserReply();
        var user = await _userManager.FindByNameAsync(request.Username);
        if (user == null)
        {
            return result;
        }
        var roles = await _userManager.GetRolesAsync(user!);
        AdminUser? customer = null;
        if (!string.IsNullOrEmpty(user!.CustomerId))
        {
            customer = await _adminRepository.GetUserById(user.CustomerId);
        }
        var coreUserVM = new CoreLibrary.Models.UserViewModel(user)
        {
            CustomerName = customer?.FullName ?? string.Empty
        };
        var userVM = new Userservice.UserViewModel
        {
            Id = Convert.ToString(user.Id),
            UserName = user.UserName,
            CustomerId = customer?.Id,
            CustomerName = customer?.FullName ?? string.Empty,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            IsLocked = coreUserVM.IsLocked
        };
        userVM.Roles.AddRange(roles);
        result.Data = userVM;
        return result;
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