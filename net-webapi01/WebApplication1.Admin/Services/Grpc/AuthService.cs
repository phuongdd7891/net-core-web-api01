using Adminauthservice;
using CoreLibrary.Const;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using AdminMicroService.Data;
using AdminMicroService.Services;
using Microsoft.AspNetCore.Identity;
using CoreLibrary.DataModels;
using MongoDB.Driver;
using Common;

namespace MicroServices.Grpc;

public class AuthGrpcService : AdminAuthServiceProto.AdminAuthServiceProtoBase
{
    private readonly AdminRepository _adminRepository;
    private readonly JwtService _jwtService;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly RoleActionRepository _roleActionRepository;
    private readonly AppDBContext _appDBContext;

    public AuthGrpcService(
        AppDBContext appDBContext,
        AdminRepository adminRepository,
        JwtService jwtService,
        RoleManager<ApplicationRole> roleManager,
        RoleActionRepository roleActionRepository
    )
    {
        _appDBContext = appDBContext;
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

    public override async Task<CommonReply> UpdateUserRole(UpdateUserRoleRequest request, ServerCallContext context)
    {
        var result = new CommonReply();
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

    public override async Task<CommonReply> DeleteUserRole(DeleteUserRoleRequest request, ServerCallContext context)
    {
        var reply = new CommonReply();
        var role = await _roleManager.FindByIdAsync(request.Id);
        if (role != null)
        {
            var client = _appDBContext.GetClient();
            using (var session = await client.StartSessionAsync())
            {
                session.StartTransaction();
                try
                {
                    var rId = Guid.Empty;
                    if (Guid.TryParse(request.Id, out rId))
                    {
                        var deleteRoleResult = await _appDBContext.AppRoles.DeleteOneAsync(session, a => a.Id == rId);
                        if (deleteRoleResult.DeletedCount == 0)
                        {
                            throw new Exception("Delete role unsuccessfully");
                        }
                        await _roleActionRepository.DeleteByRoleId(request.Id, session);
                        var users = await _appDBContext.AppUsers.Find(session, a => a.Roles.Contains(rId)).ToListAsync();
                        if (users?.Count > 0)
                        {
                            var updateTasks = users.Select(usr =>
                            {
                                usr.Roles.Remove(rId);
                                return _appDBContext.AppUsers.UpdateOneAsync(session, a => a.Id == usr.Id, Builders<ApplicationUser>.Update.Set(a => a.Roles, usr.Roles));
                            });
                            var updateResult = await Task.WhenAll(updateTasks);
                            if (updateResult.Any(a => a.ModifiedCount == 0))
                            {
                                throw new Exception("Update user role unsuccessfully");
                            }
                        }
                    }
                    await session.CommitTransactionAsync();
                }
                catch (Exception ex)
                {
                    await session.AbortTransactionAsync();
                    throw new RpcException(new Status(StatusCode.Internal, ex.Message));
                }
            }
        }
        else
        {
            reply.Message = "Role not found";
        }
        return reply;
    }
}