
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
                string token = authHeader?.Split(' ')[1] ?? string.Empty;
                headers.Add("Authorization", $"Bearer {token}");
            }
            if (HttpContext.Request.Query.ContainsKey("u"))
            {
                string username = HttpContext.Request.Query["u"]!;
                headers.Add("Username", username);
            }
            return headers;
        }
    }
}