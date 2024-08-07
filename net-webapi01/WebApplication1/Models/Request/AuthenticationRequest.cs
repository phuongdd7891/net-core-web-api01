using System.ComponentModel.DataAnnotations;

namespace WebApi.Models.Requests
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
        public string? RefreshToken { get; set; }
    }

    public class ChangePasswordRequest
    {
        public required string CurrentPassword { get; set; }
        public required string NewPassword { get; set; }
    }

    public class ConfirmEmailRequest
    {
        public string? Username { get; set; }
        public string? Token { get; set; }
    }
}