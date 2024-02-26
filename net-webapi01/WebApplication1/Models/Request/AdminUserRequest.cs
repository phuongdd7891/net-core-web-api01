using System.ComponentModel.DataAnnotations;

namespace WebApi.Models.Requests;

public class AdminUserRequest
{
    public required string Username { get; set; }
    public required string Password { get; set; }
    public string? FullName { get; set; }
    public bool IsSystem { get; set; }
    public bool IsCustomer { get; set; }
}