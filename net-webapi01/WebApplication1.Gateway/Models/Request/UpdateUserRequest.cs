namespace Gateway.Models.Requests;

public class UserRequest
{
    public required string Username { get; set; }

    public required string Email { get; set; }

    public string? Password { get; set; }

    public string? CustomerId { get; set; }

    public string? PhoneNumber { get; set; }
    public string[]? Roles { get; set; }
}