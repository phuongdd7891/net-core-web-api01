using System.Security.Claims;
using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

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

    public static ClaimsPrincipal GetClaimsPrincipal(ServerCallContext context)
    {
        if (context.UserState.TryGetValue("ClaimsPrincipal", out var claimsPrincipalObj) &&
            claimsPrincipalObj is ClaimsPrincipal claimsPrincipal)
        {
            return claimsPrincipal;
        }

        throw new RpcException(new Status(StatusCode.Unauthenticated, "Claims not found"));
    }
}