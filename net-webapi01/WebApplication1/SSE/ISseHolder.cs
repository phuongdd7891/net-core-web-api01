namespace WebApplication1.SSE;

public interface ISseHolder {
    Task AddAsync(HttpContext context);
    Task SendMessageAsync(SseMessage message);
    Task SendMessageAsync(string username, string message);
}