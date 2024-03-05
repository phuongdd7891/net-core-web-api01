using Microsoft.AspNetCore.Authentication;

namespace AdminWeb.Handler
{
    public class AuthorizationRequestHandler : DelegatingHandler
    {
        private readonly IHttpContextAccessor accessor;

        public AuthorizationRequestHandler(IHttpContextAccessor accessor)
        {
            this.accessor = accessor;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = accessor.HttpContext!.Session.GetString("Token");
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Add("Authorization", "Bearer " + token);
            }

            var username = accessor.HttpContext.Session.GetString("Username");
            if (!string.IsNullOrEmpty(username))
            {
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
