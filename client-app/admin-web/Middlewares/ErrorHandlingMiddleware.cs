using Newtonsoft.Json;
using System.Text;
using AdminWeb.Services;

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
            catch (RedirectException ex)
            {
                context.Response.Redirect(ex.RedirectUrl, ex.IsPermanent);
            }
            catch (HttpRequestException ex)
            {
                await HandleExceptionAsync(context, ex);
            }
            catch (Exception ex)
            {
                context.Response.WriteAsync(ex.Message);
            }
        }
        private static Task HandleExceptionAsync(HttpContext context, HttpRequestException ex)
        {
            if (!string.IsNullOrEmpty(context.Request.Headers["x-requested-with"]))
            {
                if (context.Request.Headers["x-requested-with"][0]!.ToLower() == "xmlhttprequest")
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
            var msg = ex.InnerException?.Message ?? ex.Message;
            StringBuilder sb = new StringBuilder();
            sb.Append("<html>");
            sb.AppendFormat("<body onload='document.forms[\"form\"].submit()'>");
            sb.AppendFormat("<form name='form' action='{0}' method='post'>", "/home/error?redirectUrl=/");
            sb.AppendFormat("<input type='hidden' name='{0}' value='{1}' />", "Message", msg);
            sb.Append("</form></body></html>");

            context.Response.WriteAsync(sb.ToString());
            return Task.CompletedTask;
        }
    }
}
