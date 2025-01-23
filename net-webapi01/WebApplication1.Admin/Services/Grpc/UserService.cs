using CoreLibrary.DataModels;
using Grpc.Core;
using Microsoft.AspNetCore.Identity;
using Userservice;
using AdminMicroService.Data;
using AdminMicroService.Models;
using MongoDB.Driver;

namespace MicroServices.Grpc;
public partial class UserGrpcService : UserServiceProto.UserServiceProtoBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly AdminRepository _adminRepository;
    private readonly RoleActionRepository _roleActionRepository;
    private readonly AppDBContext _appDBContext;
    private PasswordHasher<ApplicationUser> passwordHasher;

    public UserGrpcService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        AdminRepository adminRepository,
        RoleActionRepository roleActionRepository,
        AppDBContext appDBContext
    )
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _adminRepository = adminRepository;
        _roleActionRepository = roleActionRepository;
        _appDBContext = appDBContext;
        passwordHasher = new PasswordHasher<ApplicationUser>();
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
        var roles = await _userManager.GetRolesAsync(user);
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

    public override async Task<UpdateUserReply> UpdateUser(UpdateUserRequest request, ServerCallContext context)
    {
        var result = new UpdateUserReply();
        var client = _appDBContext.GetClient();
        using (var session = await client.StartSessionAsync())
        {
            session.StartTransaction();
            try
            {
                var appUser = await _appDBContext.AppUsers.Find(session, x => x.UserName == request.Username).FirstOrDefaultAsync();
                if (appUser != null)
                {
                    if (!string.IsNullOrEmpty(request.Password))
                    {
                        appUser.PasswordHash = passwordHasher.HashPassword(appUser, request.Password);
                    }
                    appUser!.Email = request.Email;
                    appUser.PhoneNumber = request.PhoneNumber;
                    appUser.CustomerId = request.CustomerId;
                    appUser.LockoutEnd = request.IsLocked ? DateTimeOffset.MaxValue : DateTimeOffset.UtcNow.AddDays(-1);
                    var updateResult = await _appDBContext.AppUsers.ReplaceOneAsync(session, x => x.Id == appUser.Id, appUser);
                    if (!updateResult.IsAcknowledged)
                    {
                        throw new Exception("Update user fail");
                    }
                    else
                    {
                        if (request.Roles.Count > 0)
                        {
                            if (appUser.Roles.Count > 0)
                            {
                                var removeResult = await _appDBContext.AppUsers.UpdateOneAsync(session, a => a.Id == appUser.Id, Builders<ApplicationUser>.Update.Set("Roles", new List<Guid>()));
                                if (!removeResult.IsAcknowledged)
                                {
                                    throw new Exception("Remove role fail");
                                }
                            }
                            var roleIds = await _appDBContext.AppRoles.Find(session, a => request.Roles.Contains(a.Name)).Project(a => a.Id).ToListAsync();
                            var roleResult = await _appDBContext.AppUsers.UpdateOneAsync(session, a => a.Id == appUser.Id, Builders<ApplicationUser>.Update.Set("Roles", roleIds));
                            if (!roleResult.IsAcknowledged)
                            {
                                throw new Exception("Add role fail");
                            }
                        }
                    }
                }
                else
                {
                    throw new Exception("User not found");
                }
                await session.CommitTransactionAsync();
            }
            catch (Exception ex)
            {
                await session.AbortTransactionAsync();
                throw new RpcException(new Status(StatusCode.Internal, ex.Message));
            }
        }
        return result;
    }

    public override async Task<UpdateUserReply> CreateUser(UpdateUserRequest request, ServerCallContext context)
    {
        var client = _appDBContext.GetClient();
        using (var session = await client.StartSessionAsync())
        {
            session.StartTransaction();
            var result = new UpdateUserReply();
            try
            {
                var createUser = new ApplicationUser()
                {
                    UserName = request.Username,
                    NormalizedUserName = request.Username.ToUpper(),
                    Email = request.Email,
                    PhoneNumber = request.PhoneNumber,
                    CustomerId = request.CustomerId,
                    SecurityStamp = Guid.NewGuid().ToString()
                };
                var existedUser = await _userManager.FindByNameAsync(request.Username);
                if (existedUser != null)
                {
                    throw new Exception("User existed");
                }
                var hashedPwd = passwordHasher.HashPassword(createUser, request.Password);
                createUser.PasswordHash = hashedPwd;
                await _appDBContext.AppUsers.InsertOneAsync(session, createUser);
                if (request.Roles.Count > 0)
                {
                    var roleIds = await _appDBContext.AppRoles.Find(session, a => request.Roles.Contains(a.Name)).Project(a => a.Id).ToListAsync();
                    var roleResult = await _appDBContext.AppUsers.UpdateOneAsync(session, a => a.UserName == request.Username, Builders<ApplicationUser>.Update.Set("Roles", roleIds));
                    if (!roleResult.IsAcknowledged)
                    {
                        throw new Exception("Add role fail");
                    }
                }
                await session.CommitTransactionAsync();
            }
            catch (Exception ex)
            {
                await session.AbortTransactionAsync();
                throw new RpcException(new Status(StatusCode.Internal, ex.Message));
            }
            return result;
        }
    }

    public override async Task<LockUserReply> LockUser(LockUserRequest request, ServerCallContext context)
    {
        var result = new LockUserReply();
        result.IsSuccess = false;
        var user = await _userManager.FindByNameAsync(request.Username!);
        if (user != null)
        {
            var lockResult = await _userManager.SetLockoutEndDateAsync(user, request.IsLock ? DateTimeOffset.MaxValue : DateTimeOffset.UtcNow.AddDays(-1));
            result.IsSuccess = lockResult.Succeeded;
        }
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