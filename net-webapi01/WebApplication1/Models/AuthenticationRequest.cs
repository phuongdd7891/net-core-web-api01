using System.ComponentModel.DataAnnotations;

namespace WebApplication1.ResourceModels
{
    public class AuthenticationRequest
    {
        [Required]
        public string UserName { get; set; } = string.Empty;
        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class AuthenticationResponse
    {
        public required string Token { get; set; }
        public DateTime Expiration { get; set; }
        public required string Username { get; set; }
    }

    public class UserRolesRequest
    {
        public required string Username { get; set; }
        public required string[] Roles { get; set; }
    }

    public class RoleActionRequest
    {
        public required string Action { get; set; }
        public required string Role { get; set; }
    }
}