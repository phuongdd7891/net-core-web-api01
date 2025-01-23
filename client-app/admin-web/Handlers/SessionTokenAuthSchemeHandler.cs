using AdminWeb.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace AdminWeb.Handlers
{
    public class SessionTokenAuthSchemeHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private string errMessage = "";
        public SessionTokenAuthSchemeHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder
            ) : base(options, logger, encoder)
        {
        }

        protected async override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var endpoint = Request.HttpContext.GetEndpoint();
            if (endpoint?.Metadata?.GetMetadata<IAllowAnonymous>() is object)
            {
                return AuthenticateResult.NoResult();
            }

            var authData = string.Empty;
            if (Request.HttpContext.Request.Cookies.TryGetValue(Const.AuthenticationKey, out authData))
            {
                var data = JsonConvert.DeserializeObject<AuthCookie>(authData);
                var claims = new[] {
                    new Claim(ClaimTypes.Name, data!.Username),
                    new Claim("Token", data.Token),
                    new Claim(ClaimTypes.UserData, JsonConvert.SerializeObject(data.Profile))
                };
                var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));
                var ticket = new AuthenticationTicket(principal, Scheme.Name);
                return AuthenticateResult.Success(ticket);
            }
            return AuthenticateResult.Fail(errMessage = string.Format("Authentication failed - {0}", Request.HttpContext.Request.Path));
        }

        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Response.Redirect($"/home/error?code={Const.ErrCode_InvalidToken}&redirectUrl=/");
            return Task.CompletedTask;
        }
    }

}
