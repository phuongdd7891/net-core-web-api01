namespace WebApplication1.User.Models;

public class AuthenticationResponse
{
    public required string Token { get; set; }
    public DateTime Expiration { get; set; }
    public required string Username { get; set; }
    public string? RefreshToken { get; set; }
}