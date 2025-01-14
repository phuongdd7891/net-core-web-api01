using Adminuserservice;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using WebApi.Data;
using AdminUser = Adminuserservice.AdminUser;

namespace WebApi.Services.Grpc;
public class AdminUserGrpcService : AdminUserServiceProto.AdminUserServiceProtoBase
{
    private readonly AdminRepository _adminRepository;

    public AdminUserGrpcService(
        AdminRepository adminRepository
    )
    {
        _adminRepository = adminRepository;
    }

    public override async Task<AdminUsersReply> ListUsers(ListUsersRequest request, ServerCallContext context)
    {
        var users = await _adminRepository.ListUsers(request.IsCustomer);
        var result = new AdminUsersReply();
        result.List.AddRange(users.Select(x => ConvertTo(x)));
        return result;
    }

    public override async Task<GetUserReply> GetUser(GetUserRequest request, ServerCallContext context)
    {
        var result = new GetUserReply();
        var user = await _adminRepository.GetUser(request.Username);
        if (user != null)
        {
            result.Data = ConvertTo(user);
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

    private AdminUser ConvertTo(Models.AdminUser model)
    {
        return new AdminUser
        {
            Id = model.Id,
            Username = model.Username,
            Disabled = model.Disabled,
            FullName = model.FullName ?? string.Empty,
            CreatedDate = Timestamp.FromDateTime(model.CreatedDate),
            ModifiedDate = model.ModifiedDate.HasValue ? Timestamp.FromDateTime(model.ModifiedDate.Value) : null,
            IsCustomer = model.IsCustomer,
            IsSystem = model.IsSystem,
        };
    }
}