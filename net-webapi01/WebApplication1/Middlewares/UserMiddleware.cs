using WebApi.Authentication;

namespace WebApi.Middlewares;

public class UserFilterMiddleware
{

    public UserFilterMiddleware(RequestDelegate next)
    {
        this.next = next;
    }
    public async Task Invoke(HttpContext context)
    {
        if (context.User.Identity != null && context.User.Identity.IsAuthenticated)
        {
            var userPrincipal = new UserPrincipal(new UserIdentity(context.User.Claims.ToArray()));
            context.User = userPrincipal;
        }

        await next(context);
    }
    private readonly RequestDelegate next;
}

public static class UserFilterExtension
{
    public static IApplicationBuilder UseUserFilterMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<UserFilterMiddleware>();
    }
}