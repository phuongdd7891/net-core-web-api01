using System.Net;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Newtonsoft.Json;

namespace Gateway.Interceptors;

public class ErrorHandlerInterceptor : Interceptor
{
    private readonly ILogger<ErrorHandlerInterceptor> _logger;

    public ErrorHandlerInterceptor(ILogger<ErrorHandlerInterceptor> logger)
    {
        _logger = logger;
    }

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        var call = continuation(request, context);

        return new AsyncUnaryCall<TResponse>(
            HandleResponse(call.ResponseAsync),
            call.ResponseHeadersAsync,
            call.GetStatus,
            call.GetTrailers,
            call.Dispose);
    }

    private async Task<TResponse> HandleResponse<TResponse>(Task<TResponse> inner)
    {
        try
        {
            return await inner;
        }
        catch (RpcException ex)
        {
            _logger.LogError("err from interceptor >>> {0}", ex);
            var err = ErrorStatuses.GetErrorResponse((int)HttpStatusCode.Unauthorized, ex.Status.StatusCode.ToString(), ex.Status.Detail);
            throw new InvalidOperationException(JsonConvert.SerializeObject(err), ex);
        }
    }
}