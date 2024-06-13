using System.Text.Json.Serialization;

namespace WebApplication1.SSE;

public record SseMessage
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = null!;
    [JsonPropertyName("message")]
    public string Message { get; init; } = null!;
}
public record SseClientId
{
    [JsonPropertyName("clientId")]
    public string ClientId { get; init; } = null!;
}