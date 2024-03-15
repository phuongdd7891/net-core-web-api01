using AdminWeb.Models;
using Microsoft.AspNetCore.Authentication;
using Newtonsoft.Json;

namespace AdminWeb.Handler
{
    public class AuthorizationRequestHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor _accessor;

        public AuthorizationRequestHandler(IHttpContextAccessor accessor)
        {
            _accessor = accessor;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_accessor.HttpContext!.User.Identity!.IsAuthenticated)
            {
                var token = _accessor.HttpContext.User.Claims.FirstOrDefault(a => a.Type == "Token")!.Value;
                var username = _accessor.HttpContext.User.Identity!.Name;
                request.Headers.Add("Authorization", "Bearer " + token);
                var uriBuilder = new UriBuilder(request.RequestUri!);
                if (string.IsNullOrEmpty(uriBuilder.Query))
                {
                    uriBuilder.Query = $"u={username}";
                }
                else
                {
                    uriBuilder.Query = $"{uriBuilder.Query}&u={username}";
                }
                // replace the uri in the request object
                request.RequestUri = uriBuilder.Uri;
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}
