using AdminMicroService.Models;
using CoreLibrary.Helpers;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

public class JwtValidationInterceptor : Interceptor
{
    private readonly TokenValidationParameters _tokenValidationParameters;
    private readonly HashSet<string> _excludeMethods;

    public JwtValidationInterceptor(TokenValidationParameters tokenValidationParameters, IEnumerable<string> methodsToIgnore)
    {
        _tokenValidationParameters = tokenValidationParameters;
        _excludeMethods = [.. methodsToIgnore];
    }

    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation)
    {
        // Skip token validation for specific methods
        if (_excludeMethods.Contains(context.Method))
        {
            return await continuation(request, context);
        }
        try
        {
            if (!CheckSpecialToken(context.RequestHeaders))
            {
                // Extract the Authorization header from metadata
                var token = GetJwtTokenFromMetadata(context.RequestHeaders);

                // Validate the token and extract claims
                var claimsPrincipal = ValidateToken(token);

                // Store claimsPrincipal in CallContext for service classes to access
                context.UserState["ClaimsPrincipal"] = claimsPrincipal;
            }
            else
            {
                var principal = new ClaimsPrincipal();
                var username = context.RequestHeaders.FirstOrDefault(h => h.Key == "username")!.Value;
                principal.AddIdentity(new ClaimsIdentity(new List<Claim>
                {
                    new Claim(ClaimTypes.Name, username),
                    new Claim(ClaimTypes.UserData, JsonConvert.SerializeObject(new AdminProfile
                    {
                        Username = username,
                        IsCustomer = false,
                        IsSystem = true
                    }))
                }));
                context.UserState["ClaimsPrincipal"] = principal;
            }
        }
        catch (SecurityTokenException ex)
        {
            throw new RpcException(new Status(StatusCode.Unauthenticated, $"[{context.Method}] {ex.Message}"));
        }
        catch (Exception ex)
        {
            throw new RpcException(new Status(StatusCode.Internal, $"[{context.Method}] {ex.Message}"));
        }

        // Proceed to the next handler in the pipeline
        return await continuation(request, context);
    }

    private string GetJwtTokenFromMetadata(Metadata headers)
    {
        var authHeader = headers.FirstOrDefault(h => h.Key == "authorization")?.Value;

        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            throw new SecurityTokenException("Missing or invalid Authorization header");
        }

        return authHeader.Substring("Bearer ".Length);
    }

    private ClaimsPrincipal ValidateToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();

        try
        {
            var principal = tokenHandler.ValidateToken(token, _tokenValidationParameters, out var validatedToken);

            // Ensure the token is a valid JWT
            if (validatedToken is not JwtSecurityToken jwtToken || jwtToken.Header.Alg != SecurityAlgorithms.HmacSha256Signature)
            {
                throw new SecurityTokenException("Invalid token");
            }

            return principal;
        }
        catch
        {
            throw new Exception("Token validation failed");
        }
    }

    private bool CheckSpecialToken(Metadata headers)
    {
        var specialAuth = headers.FirstOrDefault(h => h.Key == "specialauthorization")?.Value;
        if (!string.IsNullOrEmpty(specialAuth))
        {
            var specialToken = specialAuth.ToString();
            var username = headers.FirstOrDefault(h => h.Key == "username")?.Value;
            if (string.IsNullOrEmpty(username))
            {
                throw new SecurityTokenException("Missing username in special authorization header");
            }
            if (string.Compare(specialToken, AESHelpers.Encrypt($"{username}:{DateTime.UtcNow.AddMinutes(5):yyyy-MM-dd HH:mm}")) != 0)
            {
                throw new SecurityTokenException("Invalid token");
            }
            return true;
        }
        return false;
    }
}
