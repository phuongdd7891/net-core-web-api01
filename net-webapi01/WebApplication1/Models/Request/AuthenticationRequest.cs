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

    public class UserRolesRequest
    {
        public required string Username { get; set; }
        public required string[] Roles { get; set; }
    }

    public class RoleActionRequest
    {
        public required string[] Actions { get; set; }
        public required string RoleId { get; set; }
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

    public class LockUserRequest
    {
        public string? Username { get; set; }
        public bool IsLock { get; set; }
    }

    public class ApplicationRoleRequest
    {
        public string? Id { get; set; }
        public required string Name { get; set; }
        public string? CustomerId { get; set; }

        public string StoreName
        {
            get
            {
                return string.Format("{0}{1}", string.IsNullOrEmpty(CustomerId) || Name.StartsWith($"{CustomerId}__") ? "" : $"{CustomerId}__", Name);
            }
        }
    }
}