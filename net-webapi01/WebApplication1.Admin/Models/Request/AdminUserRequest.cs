using System.ComponentModel.DataAnnotations;

namespace WebApi.Models.Requests;

public class AdminUserRequest
{
    public required string Username { get; set; }
    public required string Password { get; set; }
    public required string FullName { get; set; }
    public required string Email { get; set; }
    public bool IsSystem { get; set; }
    public bool IsCustomer { get; set; }
    public bool Disabled { get; set; }
}

public class AdminRefreshTokenRequest
{
    public required string AccessToken { get; set; }
    public required string RefreshToken { get; set; }
}