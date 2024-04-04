using AdminWeb.Models.Response;

namespace AdminWeb.Models
{
    public class UserViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public bool IsLocked { get; set; }
        public string[]? Roles { get; set; }
        public string? Password { get; set; }
    }

    public class AuthCookie
    {
        public string Token { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public AdminProfile? Profile { get; set; }
    }
}
