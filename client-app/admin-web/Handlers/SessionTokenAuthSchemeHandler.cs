﻿using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using AdminWeb.Models.Response;

namespace AdminWeb.Handlers
{
    public class SessionTokenAuthSchemeHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private string errMessage = "";
        public SessionTokenAuthSchemeHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock) : base(options, logger, encoder, clock)
        {
        }

        protected async override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var username = Request.HttpContext.Session.GetString("Username");
            if (!string.IsNullOrEmpty(username))
            {
                var claims = new[] { new Claim(ClaimTypes.Name, username) };
                var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, Scheme.Name));
                var ticket = new AuthenticationTicket(principal, Scheme.Name);
                return AuthenticateResult.Success(ticket);
            }
            return AuthenticateResult.Fail(errMessage = "Authentication failed");
        }

        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = 401;
            Response.ContentType = "application/json";
            Response.WriteAsync(errMessage);

            return Task.CompletedTask;
        }
    }

}