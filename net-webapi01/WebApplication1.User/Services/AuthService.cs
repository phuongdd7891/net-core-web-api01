using Grpc.Core;
using Managementauthservice;
using Microsoft.AspNetCore.Identity;
using CoreLibrary.DataModels;
using CoreLibrary.Const;
using Google.Protobuf.WellKnownTypes;
using Common;

namespace WebApplication1.User.Services;

public class AuthService : ManagementAuthServiceProto.ManagementAuthServiceProtoBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtService _jwtService;
    public AuthService(
        UserManager<ApplicationUser> userManager,
        JwtService jwtService
    )
    {
        _userManager = userManager;
        _jwtService = jwtService;
    }

    public override async Task<LoginReply> Login(LoginRequest request, ServerCallContext context)
    {
        var reply = new LoginReply();
        var user = await _userManager.FindByNameAsync(request.Username);
        if (user == null)
        {
            reply.ErrorCode = Const.ErrCode_UserNotFound;
        }
        else
        {
            var isPasswordValid = await _userManager.CheckPasswordAsync(user!, request.Password);
            if (!isPasswordValid)
            {
                reply.ErrorCode = Const.ErroCode_BadCredential;
            }
            else if (user!.LockoutEnd.HasValue && DateTimeOffset.Compare(user!.LockoutEnd.Value, DateTimeOffset.UtcNow) > 0)
            {
                reply.ErrorCode = Const.ErrCode_DisabledAccount;
            }
            else
            {
                var token = await _jwtService.CreateToken(user);
                reply.AuthData = new AuthData
                {
                    Token = token.Token,
                    Expiration = Timestamp.FromDateTime(token.Expiration),
                    Username = user.UserName
                };
            }
        }
        return reply;
    }

    public override async Task<CommonReply> ChangePassword(ChangePasswordRequest request, ServerCallContext context)
    {
        var reply = new CommonReply();
        var user = await _userManager.FindByNameAsync(request.Username);
        if (user == null)
        {
            reply.Message = "User not found";
        }
        else
        {
            var result = await _userManager.ChangePasswordAsync(user, request.OldPassword, request.NewPassword);
            if (!result.Succeeded)
            {
                reply.Message = result.Errors.FirstOrDefault()?.Description ?? "Change password failed";
            }
        }
        return reply;
    }
}
