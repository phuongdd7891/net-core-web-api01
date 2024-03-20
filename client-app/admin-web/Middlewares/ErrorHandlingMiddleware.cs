using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Newtonsoft.Json;
using System.Net;
using System.Security.Policy;

namespace AdminWeb.Middlewares
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        public ErrorHandlingMiddleware(RequestDelegate next)
        {
            _next = next;
        }
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (HttpRequestException ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }
        private static Task HandleExceptionAsync(HttpContext context, HttpRequestException ex)
        {
            if (!string.IsNullOrEmpty(context.Request.Headers["x-requested-with"]))
            {
                if (context.Request.Headers["x-requested-with"][0]!
                    .ToLower() == "xmlhttprequest")
                {
                    var result = JsonConvert.SerializeObject(new { error = ex.InnerException?.Message ?? ex.Message });
                    context.Response.ContentType = "application/json";
                    context.Response.StatusCode = Convert.ToInt32(ex.StatusCode);
                    if (ex.Message == Const.ErrCode_InvalidToken || ex.Message == Const.ErrCode_TokenExpired)
                    {
                        result = $"window.location.href = '/home/error?code={ex.Message}&redirectUrl=/'";
                        context.Response.ContentType = "application/javascript";
                        context.Response.StatusCode = 200;
                    }
                    return context.Response.WriteAsync(result);
                }
            }
            context.Response.Redirect($"/home/error");
            return Task.CompletedTask;
        }
    }
}
