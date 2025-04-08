using Grpc.Core;
using Grpc.Core.Interceptors;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Gateway.Interceptors;

public class LoggingInterceptor : Interceptor
{
    private readonly ILogger _logger;

    public LoggingInterceptor(ILogger logger)
    {
        _logger = logger.ForContext<LoggingInterceptor>();
    }

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        _logger.Information("gRPC Request: Method={Method}, Request={@Request}, Headers={@Headers}",
            context.Method.FullName, request, context.Options.Headers);

        var call = continuation(request, context);

        return new AsyncUnaryCall<TResponse>(
            HandleResponse<TRequest, TResponse>(call.ResponseAsync, context),
            call.ResponseHeadersAsync,
            call.GetStatus,
            call.GetTrailers,
            call.Dispose);
    }

    private async Task<TResponse> HandleResponse<TRequest, TResponse>(
        Task<TResponse> responseTask,
        ClientInterceptorContext<TRequest, TResponse> context) 
        where TRequest : class 
        where TResponse : class
    {
        try
        {
            var response = await responseTask;
            return response;
        }
        catch (RpcException ex)
        {
            _logger.Error(ex, "gRPC Error: Method={Method}, StatusCode={StatusCode}, Detail={Detail}",
                context.Method.FullName, ex.StatusCode, ex.Status.Detail);
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "gRPC Unexpected Error: Method={Method}", context.Method.FullName);
            throw;
        }
    }
}