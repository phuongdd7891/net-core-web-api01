// ErrorHandlingMiddleware.cs
using System.Net;
using Newtonsoft.Json;

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
        catch (Exception ex)
        {
            // Log the exception
            var errResp = JsonConvert.DeserializeObject<ErrorResponse>(ex.Message);
            // Return consistent error response
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = errResp?.Code ?? (int)HttpStatusCode.InternalServerError;

            if (errResp != null)
            {
                errResp.Message = errResp?.Message ?? "An error occurred while processing your request.";
            }

            var json = JsonConvert.SerializeObject(errResp);
            await context.Response.WriteAsync(json);
        }
    }
}

public static class ErrorHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseErrorHandling(
        this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ErrorHandlingMiddleware>();
    }
}