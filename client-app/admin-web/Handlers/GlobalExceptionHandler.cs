using AdminWeb;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using System.Net;
using System.Text;

namespace AdminWeb.Handlers
{
    internal sealed class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            if (exception is not HttpRequestException httpRequestException)
            {
                return false;
            }

            _logger.LogError(
                httpRequestException, "Exception occurred: {Message}", httpRequestException.Message);

            var problemDetails = new ProblemDetails
            {
                Status = Convert.ToInt32(httpRequestException.StatusCode),
                Title = httpRequestException.InnerException?.Message ?? httpRequestException.Message
            };
            httpContext.Response.StatusCode = problemDetails.Status.Value;
            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

            return true;
        }
    }
}