namespace AdminMicroService.Models;

public class User
{
    public required string Username { get; set; }

    public required string Email { get; set; }

    public string? Password { get; set; }

    public string? CustomerId { get; set; }

    public string? PhoneNumber { get; set; }
}

public class UserViewModel
{
    public Guid Id { get; set; }
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsLocked { get; set; }
    public string[]? Roles { get; set; }
    public List<Guid>? RoleIds { get; set; }
    public string? CustomerId { get; set; }
    public string? CustomerName { get; set; }
}