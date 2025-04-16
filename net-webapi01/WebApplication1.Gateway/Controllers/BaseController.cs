
using CoreLibrary.Helpers;
using Gateway.Authorization;
using Grpc.Core;
using Microsoft.AspNetCore.Mvc;

namespace Gateway.Controllers;

public class BaseController: ControllerBase
{
    public Metadata DefaultHeader
    {
        get {
            var headers = new Metadata();
            if (HttpContext.Request.Headers.ContainsKey("Authorization"))
            {
                string? authHeader = HttpContext.Request.Headers["Authorization"];
                if (authHeader != null)
                {
                    headers.Add("Authorization", authHeader);
                }
            }
            if (HttpContext.Request.Query.ContainsKey("u"))
            {
                string username = HttpContext.Request.Query["u"]!;
                headers.Add("Username", username);
            }
            return headers;
        }
    }

    public Metadata GetSpecialHeader(string username)
    {
        var headers = new Metadata();
        var token = AESHelpers.Encrypt($"{username}:{DateTime.UtcNow.AddMinutes(5):yyyy-MM-dd HH:mm}");
        headers.Add("SpecialAuthorization", token);
        headers.Add("Username", username);
        return headers;
    }
}