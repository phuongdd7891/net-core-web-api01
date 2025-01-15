
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;

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
}