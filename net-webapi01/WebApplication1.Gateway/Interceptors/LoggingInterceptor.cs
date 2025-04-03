using Grpc.Core;
using Grpc.Core.Interceptors;
using Serilog.Context;

namespace Gateway.Interceptors;

public class LoggingInterceptor : Interceptor
{
    private readonly Serilog.ILogger _logger;

    public LoggingInterceptor(Serilog.ILogger logger)
    {
        _logger = logger.ForContext<LoggingInterceptor>();
    }

    public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
    {
        var startTime = DateTime.UtcNow;
        var requestId = Guid.NewGuid().ToString();

        // Log the request with metadata
        using (LogContext.PushProperty("RequestId", requestId))
        {
            _logger.Information("gRPC Request: Method={Method}, Host={Host}, Request={@Request}",
                context.Method.FullName, context.Host, request);

            var call = continuation(request, context);

            return new AsyncUnaryCall<TResponse>(
                HandleResponse(call.ResponseAsync, (ClientInterceptorContext<object, TResponse>)(object)context, startTime, requestId),
                call.ResponseHeadersAsync,
                call.GetStatus,
                call.GetTrailers,
                call.Dispose);
        }
    }

    public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
        TRequest request,
        ClientInterceptorContext<TRequest, TResponse> context,
        AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation)
    {
        var startTime = DateTime.UtcNow;
        var requestId = Guid.NewGuid().ToString();

        using (LogContext.PushProperty("RequestId", requestId))
        {
            _logger.Information("gRPC Server Streaming Request: Method={Method}, Host={Host}, Request={@Request}",
                context.Method.FullName, context.Host, request);

            var call = continuation(request, context);

            return new AsyncServerStreamingCall<TResponse>(
                new AsyncStreamReader<TResponse>(call.ResponseStream, (ClientInterceptorContext<object, TResponse>)(object)context, startTime, requestId, _logger),
                call.ResponseHeadersAsync,
                call.GetStatus,
                call.GetTrailers,
                call.Dispose);
        }
    }

    private async Task<TResponse> HandleResponse<TResponse> (
        Task<TResponse> responseTask,
        ClientInterceptorContext<object, TResponse> context,
        DateTime startTime,
        string requestId) where TResponse : class
    {
        try
        {
            var response = await responseTask;
            var duration = DateTime.UtcNow - startTime;

            using (LogContext.PushProperty("RequestId", requestId))
            {
                _logger.Information("gRPC Response: Method={Method}, Duration={DurationMs}ms, Response={@Response}",
                    context.Method.FullName, duration.TotalMilliseconds, response);
            }
            return response;
        }
        catch (RpcException ex)
        {
            var duration = DateTime.UtcNow - startTime;
            using (LogContext.PushProperty("RequestId", requestId))
            {
                _logger.Error(ex, "gRPC Error: Method={Method}, Duration={DurationMs}ms, StatusCode={StatusCode}, Detail={Detail}",
                    context.Method.FullName, duration.TotalMilliseconds, ex.StatusCode, ex.Status.Detail);
            }
            throw;
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            using (LogContext.PushProperty("RequestId", requestId))
            {
                _logger.Error(ex, "gRPC Unexpected Error: Method={Method}, Duration={DurationMs}ms",
                    context.Method.FullName, duration.TotalMilliseconds);
            }
            throw;
        }
    }

    private class AsyncStreamReader<TResponse> : IAsyncStreamReader<TResponse> where TResponse : class
    {
        private readonly IAsyncStreamReader<TResponse> _inner;
        private readonly ClientInterceptorContext<object, TResponse> _context;
        private readonly DateTime _startTime;
        private readonly string _requestId;
        private readonly Serilog.ILogger _logger;

        public AsyncStreamReader(
            IAsyncStreamReader<TResponse> inner,
            ClientInterceptorContext<object, TResponse> context,
            DateTime startTime,
            string requestId,
            Serilog.ILogger logger)
        {
            _inner = inner;
            _context = context;
            _startTime = startTime;
            _requestId = requestId;
            _logger = logger;
        }

        public TResponse Current => _inner.Current;

        public async Task<bool> MoveNext(CancellationToken cancellationToken)
        {
            var result = await _inner.MoveNext(cancellationToken);
            if (result)
            {
                using (LogContext.PushProperty("RequestId", _requestId))
                {
                    _logger.Information("gRPC Server Streaming Response: Method={Method}, Response={@Response}",
                        _context.Method.FullName, _inner.Current);
                }
            }
            else
            {
                var duration = DateTime.UtcNow - _startTime;
                using (LogContext.PushProperty("RequestId", _requestId))
                {
                    _logger.Information("gRPC Server Streaming Completed: Method={Method}, Duration={DurationMs}ms",
                        _context.Method.FullName, duration.TotalMilliseconds);
                }
            }
            return result;
        }
    }
}