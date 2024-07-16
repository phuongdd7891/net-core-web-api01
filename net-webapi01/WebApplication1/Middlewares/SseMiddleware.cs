using WebApi.SSE;

namespace WebApi.Middlewares;

public static class SseHolderMapper
{
    public static IApplicationBuilder MapSseHolder(this IApplicationBuilder app, PathString path)
    {
        return app.Map(path, (app) => app.UseMiddleware<SseMiddleware>());
    }
}
public class SseMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ISseHolder _sse;
    public SseMiddleware(RequestDelegate next, ISseHolder sse)
    {
        _next = next;
        _sse = sse;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _sse.AddAsync(context);
        await _next(context);
    }
}