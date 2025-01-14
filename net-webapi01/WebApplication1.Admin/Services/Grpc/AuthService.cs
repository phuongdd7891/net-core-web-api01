using Adminauthservice;
using CoreLibrary.Const;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using WebApi.Data;

namespace WebApi.Services.Grpc;

public class AuthGrpcService : AdminAuthServiceProto.AdminAuthServiceProtoBase
{
    private readonly AdminRepository _adminRepository;
    private readonly JwtService _jwtService;

    public AuthGrpcService(
        AdminRepository adminRepository,
        JwtService jwtService
    )
    {
        _adminRepository = adminRepository;
        _jwtService = jwtService;
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
}