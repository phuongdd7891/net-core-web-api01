using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace AdminMicroService.Models
{   
    public class AdminUser
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        public required string Username { get; set; }
        public required string Password { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public bool IsSystem { get; set; }
        public bool IsCustomer { get; set; }
        public bool Disabled { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiryDate { get; set; }

        public string ToClaimData()
        {
            return JsonConvert.SerializeObject(new AdminProfile
            { 
                Id = Id,
                Username = Username,
                IsCustomer = IsCustomer,
                IsSystem = IsSystem
            });
        }
    }

    public class AdminProfile
    {
        public string? Id { get; set; }
        public required string Username { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public bool IsSystem { get; set; }
        public bool IsCustomer { get; set; }
        public bool Disabled { get; set; }
        public DateTime CreatedDate { get; set; }
        public int UserCount { get; set; }
    }

    public class GetUsersReply
    {
        public List<UserViewModel>? List { get; set; }
        public int Total { get; set; }
    }
}