// ErrorHandlingMiddleware.cs
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(
        RequestDelegate next,
        ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Somethings error");
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
                var errResp = JsonConvert.DeserializeObject<ErrorStatusResponse>(ex.Message);
                context.Response.StatusCode = errResp!.StatusCode;
                var json = JsonConvert.SerializeObject(errResp.Data, jsonSettings);
                await context.Response.WriteAsync(json);
            }
            catch
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                var json = JsonConvert.SerializeObject(new DataResponse<string> {
                    Code = DataResponseCode.IternalError.ToString(),
                    Data = ex.Message
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