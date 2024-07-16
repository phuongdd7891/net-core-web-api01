
using System.Collections.Concurrent;
using System.Text.Json;
using CoreLibrary.Repository;

namespace WebApi.SSE;

public record SseClient(HttpResponse Response, CancellationTokenSource Cancel);

public class SseHolder : ISseHolder
{
    private readonly ILogger<SseHolder> _logger;
    private readonly RedisRepository _redisRepository;
    private readonly ConcurrentDictionary<string, SseClient> clients = new();
    private readonly string SseKey = "sse-clients";
    private readonly Dictionary<string, string> memClients;

    public SseHolder(
        ILogger<SseHolder> logger,
        RedisRepository redisRepository,
        IHostApplicationLifetime applicationLifetime)
    {
        _logger = logger;
        _redisRepository = redisRepository;
        applicationLifetime.ApplicationStopping.Register(OnShutdown);
        if (!_redisRepository.HasKey(SseKey).Result)
        {
            memClients = new Dictionary<string, string>();
            _redisRepository.SetHashEntity(SseKey, memClients).ConfigureAwait(false);
        }
        else
        {
            memClients = _redisRepository.GetHashEntity<string>(SseKey).Result;
        }
    }

    public async Task AddAsync(HttpContext context)
    {
        var clientId = CreateId();
        var cancel = new CancellationTokenSource();
        var client = new SseClient(Response: context.Response, Cancel: cancel);
        if (clients.TryAdd(clientId, client))
        {
            var username = context.Request.Query["u"];
            if (!string.IsNullOrEmpty(username))
            {
                memClients[username!] = clientId;
                await _redisRepository.SetHashEntity(SseKey, memClients);
            }

            EchoAsync(clientId, client);
            context.RequestAborted.WaitHandle.WaitOne();
            RemoveClient(clientId, username);
            await Task.FromResult(true);
        }
    }

    public async Task SendMessageAsync(SseMessage message)
    {
        foreach (var c in clients)
        {
            if (c.Key == message.Id)
            {
                var messageJson = JsonSerializer.Serialize(message);
                await c.Value.Response.WriteAsync($"data: {messageJson}\r\r", c.Value.Cancel.Token);
                await c.Value.Response.Body.FlushAsync(c.Value.Cancel.Token);
            }
        }
    }

    public async Task SendMessageAsync(string username, string message)
    {
        SseClient? client;
        if (memClients.ContainsKey(username))
        {
            var clientId = memClients[username];
            if (clients.TryGetValue(clientId, out client))
            {
                var messageJson = JsonSerializer.Serialize(new SseMessage
                {
                    Id = clientId,
                    Message = message
                });
                await client.Response.WriteAsync($"data: {messageJson}\r\r", client.Cancel.Token);
                await client.Response.Body.FlushAsync(client.Cancel.Token);
            }
        }
    }

    private async void EchoAsync(string clientId, SseClient client)
    {
        try
        {
            var clientIdJson = JsonSerializer.Serialize(new SseClientId { ClientId = clientId });
            client.Response.Headers.Add("Content-Type", "text/event-stream");
            client.Response.Headers.Add("Cache-Control", "no-cache");
            client.Response.Headers.Add("Connection", "keep-alive");
            // Send ID to client-side after connecting
            await client.Response.WriteAsync($"data: {clientIdJson}\r\r", client.Cancel.Token);
            await client.Response.Body.FlushAsync(client.Cancel.Token);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError($"Exception {ex.Message}");
        }

    }
    private void OnShutdown()
    {
        var tmpClients = new List<KeyValuePair<string, SseClient>>();
        foreach (var c in clients)
        {
            c.Value.Cancel.Cancel();
            tmpClients.Add(c);
        }
        foreach (var c in tmpClients)
        {
            clients.TryRemove(c);
        }
        _redisRepository.Remove(SseKey).ConfigureAwait(false);
    }
    public async void RemoveClient(string id, string? username)
    {
        var target = clients.FirstOrDefault(c => c.Key == id);
        if (string.IsNullOrEmpty(target.Key))
        {
            return;
        }
        target.Value.Cancel.Cancel();
        clients.TryRemove(target);
        if (!string.IsNullOrEmpty(username))
        {
            memClients.Remove(username);
            await _redisRepository.DeleteHashByField(SseKey, username);
        }
    }
    private string CreateId()
    {
        return Guid.NewGuid().ToString();
    }
}