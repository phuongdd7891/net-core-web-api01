using System.Security.Claims;
using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using AdminMicroService.Models;

public class Helpers
{
    public static JsonResult GetUnauthorizedResult(DataResponse<string> data)
    {
        var jsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            }
        };
        return new JsonResult(data, jsonSettings)
        {
            StatusCode = StatusCodes.Status401Unauthorized
        };
    }

    public static AdminProfile? GetClaimProfile(ServerCallContext context)
    {
        var claimsPrincipal = GetClaimsPrincipal(context);
        var userData = JsonConvert.DeserializeObject<AdminProfile>(claimsPrincipal.FindFirst(ClaimTypes.UserData)?.Value ?? string.Empty);
        return userData;
    }

    private static ClaimsPrincipal GetClaimsPrincipal(ServerCallContext context)
    {
        if (context.UserState.TryGetValue("ClaimsPrincipal", out var claimsPrincipalObj) &&
            claimsPrincipalObj is ClaimsPrincipal claimsPrincipal)
        {
            return claimsPrincipal;
        }

        throw new RpcException(new Status(StatusCode.Unauthenticated, "Claims not found"));
    }
}