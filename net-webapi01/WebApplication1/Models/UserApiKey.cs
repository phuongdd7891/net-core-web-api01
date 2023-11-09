using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace IdentityMongo.Models
{
    public class UserApiKey
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonRequired]
        public string Value { get; set; } = string.Empty;

        [BsonRequired]
        [BsonElement]
        public string Username { get; set; } = string.Empty;

        [BsonRequired]
        public ApplicationUser? User { get; set; }

        [BsonElement]
        public DateTime LoginTime { get; set; }
    }
}