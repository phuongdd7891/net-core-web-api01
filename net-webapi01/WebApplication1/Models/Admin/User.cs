using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WebApi.Models.Admin
{
    public class AdminUser
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("Name")]
        [Newtonsoft.Json.JsonProperty("name")]
        public string? FullName { get; set; }

    }
}