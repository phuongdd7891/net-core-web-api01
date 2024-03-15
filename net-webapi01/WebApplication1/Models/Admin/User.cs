using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace WebApi.Models.Admin
{
    public class AdminUser
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        public required string Username { get; set; }
        public required string Password { get; set; }
        public string? FullName { get; set; }
        public bool IsSystem { get; set; }
        public bool IsCustomer { get; set; }
        public DateTime CreatedDate { get; set; }
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
        public bool IsSystem { get; set; }
        public bool IsCustomer { get; set; }
    }
}