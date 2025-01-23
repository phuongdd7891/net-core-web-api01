using Adminuserservice;
using CoreLibrary.DataModels;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Identity;
using AdminMicroService.Data;

namespace MicroServices.Grpc;
public class AdminUserGrpcService : AdminUserServiceProto.AdminUserServiceProtoBase
{
    private readonly AdminRepository _adminRepository;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminUserGrpcService(
        UserManager<ApplicationUser> userManager,
        AdminRepository adminRepository
    )
    {
        _userManager = userManager;
        _adminRepository = adminRepository;
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

    public override async Task<GetCustomerUsersReply> GetCustomerUsers(Empty request, ServerCallContext context)
    {
        var users = new List<AdminMicroService.Models.AdminUser>();
        var claimUser = Helpers.GetClaimProfile(context);
        if (claimUser != null)
        {
            if (claimUser.IsSystem)
            {
                var listUsers = await _adminRepository.ListUsers(true);
                users.AddRange(listUsers);
            }
            if (claimUser.IsCustomer)
            {
                users.Add(await _adminRepository.GetUser(claimUser.Username));
            }
        }
        var appUsers = _userManager.Users;
        var list = users.GroupJoin(appUsers, u => u.Id, a => a.CustomerId, (u, a) => new { Admin = u, UserCount = a.Count() }).ToList();
        var reply = new GetCustomerUsersReply();
        reply.Data.AddRange(list.Select(x => new AdminProfile
        {
            Id = x.Admin.Id,
            Username = x.Admin.Username,
            FullName = x.Admin.FullName,
            IsCustomer = x.Admin.IsCustomer,
            IsSystem = x.Admin.IsSystem,
            Email = x.Admin.Email,
            Disabled = x.Admin.Disabled,
            CreatedDate = Timestamp.FromDateTime(x.Admin.CreatedDate),
            UserCount = x.UserCount,
        }));
        return reply;
    }

    public override async Task<CreateUserReply> CreateUser(CreateUserRequest request, ServerCallContext context)
    {
        var result = new CreateUserReply();
        var user = await _adminRepository.GetUser(request.Username);
        if (user != null)
        {
            result.Message = "Username already exists";
        }
        else
        {
            await _adminRepository.CreateUser(new AdminMicroService.Models.AdminUser
            {
                Username = request.Username,
                Password = request.Password,
                FullName = request.FullName,
                Email = request.Email,
                IsSystem = request.IsSystem,
                IsCustomer = request.IsCustomer,
                Disabled = request.Disabled
            });
        }
        return result;
    }

    public override async Task<CreateUserReply> UpdateUser(UpdateUserRequest request, ServerCallContext context)
    {
        var result = new CreateUserReply();
        var user = await _adminRepository.GetUser(request.Username);
        if (user == null)
        {
            result.Message = "User not found";
        }
        else
        {
            await _adminRepository.UpdateUser(new AdminMicroService.Models.AdminUser
            {
                Id = user.Id,
                Username = request.Username,
                Password = String.Equals(request.Password, "pwd", StringComparison.OrdinalIgnoreCase) ? user.Password : request.Password,
                FullName = request.FullName,
                Email = request.Email,
                Disabled = request.Disabled,
                CreatedDate = user.CreatedDate
            });
        }
        return result;
    }

    public override async Task<AdminUser?> GetUser(GetUserProfileRequest request, ServerCallContext context)
    {
        var user = await _adminRepository.GetUser(request.Username);
        if (user == null)
        {
            return null;
        }
        return new AdminUser
        {
            Id = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            Email = user.Email,
            IsSystem = user.IsSystem,
            IsCustomer = user.IsCustomer,
            Disabled = user.Disabled,
            CreatedDate = Timestamp.FromDateTime(user.CreatedDate)
        };
    }
}