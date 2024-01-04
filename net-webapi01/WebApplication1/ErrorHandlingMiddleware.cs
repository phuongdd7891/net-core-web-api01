// ErrorHandlingMiddleware.cs
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

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
            context.Response.ContentType = "application/json";
            var jsonSettings = new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy()
                }
            };
            try
            {
                // Log the exception
                var errResp = JsonConvert.DeserializeObject<DataResponse<string>>(ex.Message);
                // Return consistent error response
                context.Response.StatusCode = errResp?.Code ?? (int)HttpStatusCode.InternalServerError;

                if (errResp != null)
                {
                    errResp.Data = errResp?.Data ?? "An error occurred while processing your request.";
                }

                var json = JsonConvert.SerializeObject(errResp, jsonSettings);
                await context.Response.WriteAsync(json);
            }
            catch (Exception ex2)
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                var json = JsonConvert.SerializeObject(new DataResponse<string> {
                    Code = context.Response.StatusCode,
                    Data = ex2.Message
                }, jsonSettings);
                await context.Response.WriteAsync(json);
            }
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