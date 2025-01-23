using Adminauthservice;
using CoreLibrary.Const;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using AdminMicroService.Data;
using AdminMicroService.Services;
using Microsoft.AspNetCore.Identity;
using CoreLibrary.DataModels;

namespace MicroServices.Grpc;

public class AuthGrpcService : AdminAuthServiceProto.AdminAuthServiceProtoBase
{
    private readonly AdminRepository _adminRepository;
    private readonly JwtService _jwtService;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly RoleActionRepository _roleActionRepository;

    public AuthGrpcService(
        AdminRepository adminRepository,
        JwtService jwtService,
        RoleManager<ApplicationRole> roleManager,
        RoleActionRepository roleActionRepository
    )
    {
        _adminRepository = adminRepository;
        _jwtService = jwtService;
        _roleManager = roleManager;
        _roleActionRepository = roleActionRepository;
    }

    public override async Task<AdminLoginReply> Login(AdminLoginRequest request, ServerCallContext context)
    {
        var result = new AdminLoginReply();
        var user = await _adminRepository.GetUser(request.Username);
        if (user == null)
        {
            result.ErrorCode = Const.ErrCode_UserNotFound;
        }
        else if (!user.IsSystem && !user.IsCustomer)
        {
            result.ErrorCode = Const.ErrCode_InvalidUser;
        }
        else if (user.Disabled)
        {
            result.ErrorCode = Const.ErrCode_DisabledAccount;
        }
        else
        {
            var validPassword = await _adminRepository.VerifyPassword(request.Username, request.Password);
            if (validPassword)
            {
                var refreshToken = _jwtService.GenerateRefreshToken();
                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryDate = DateTime.UtcNow.AddDays(7);
                await _adminRepository.UpdateUser(user);
                var token = await _jwtService.CreateAdminToken(user!);
                result.Username = token.Username;
                result.Token = token.Token;
                result.RefreshToken = refreshToken;
                result.Expiration = Timestamp.FromDateTime(token.Expiration);
            }
            else
            {
                result.ErrorCode = Const.ErroCode_BadCredential;
            }
        }
        return result;
    }

    public override async Task<CreateUserRoleReply> CreateUserRole(CreateUserRoleRequest request, ServerCallContext context)
    {
        var result = new CreateUserRoleReply();
        IdentityResult identityResult = await _roleManager.CreateAsync(new ApplicationRole()
        {
            Name = request.Name,
            CustomerId = string.IsNullOrEmpty(request.CustomerId) ? null : request.CustomerId,
        });
        if (identityResult.Succeeded)
        {
            var createdRole = await _roleManager.FindByNameAsync(request.Name);
            result.Id = createdRole!.Id.ToString();
        }
        else
        {
            result.Message = identityResult.Errors.FirstOrDefault()?.Code ?? "";
        }
        return result;
    }

    public override async Task<Empty> AddActionsToRole(AddActionsToRoleRequest request, ServerCallContext context)
    {
        var loginUser = Helpers.GetClaimProfile(context);
        await _roleActionRepository.AddActionsToRole(request.RoleId, request.Actions.ToArray(), loginUser?.Username ?? "");
        return new Empty();
    }

    public override async Task<UpdateUserRoleReply> UpdateUserRole(UpdateUserRoleRequest request, ServerCallContext context)
    {
        var result = new UpdateUserRoleReply();
        var role = await _roleManager.FindByIdAsync(request.Id);
        if (role == null)
        {
            result.Message = "Role not found";
        }
        else
        {
            role.CustomerId = request.CustomerId;
            role.Name = request.Name;
            var tasks = new List<Task<IdentityResult>>
            {
                _roleManager.UpdateAsync(role),
                _roleManager.SetRoleNameAsync(role, request.Name)
            };
            var results = await Task.WhenAll(tasks);
            if (results.Any(a => !a.Succeeded))
            {
                var error = results.FirstOrDefault(a => !a.Succeeded);
                result.Message = error!.Errors.FirstOrDefault()?.Description ?? "";
            }
        }
        return result;
    }
}