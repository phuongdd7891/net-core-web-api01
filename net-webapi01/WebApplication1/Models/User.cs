using System.ComponentModel.DataAnnotations;
 
namespace WebApi.Models
{
    public class User
    {
        public required string Username { get; set; }
 
        public required string Email { get; set; }
 
        public string? Password { get; set; }

        public string? CustomerId { get; set; }

        public string? PhoneNumber { get; set; }
    }

    public class GetUsersReply
    {
        public List<UserViewModel>? List { get; set; }
        public int Total { get; set; }
    }

    public class UserViewModel
    {
        public Guid Id { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public bool IsLocked { get; set; }
        public string[]? Roles { get; set; }
    }
}