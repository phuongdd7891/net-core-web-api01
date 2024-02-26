using System.ComponentModel.DataAnnotations;
 
namespace IdentityMongo.Models
{
    public class User
    {
        [Required]
        public required string Username { get; set; }
 
        [Required]
        [EmailAddress(ErrorMessage = "Invalid Email")]
        public required string Email { get; set; }
 
        [Required]
        public required string Password { get; set; }

        public string? CustomerId { get; set; }
    }
}