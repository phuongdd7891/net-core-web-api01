using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CoreLibrary.Models;

public class RoleAction {
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    public required string RequestAction { get; set; }
    public required List<string> Roles { get; set; }
    public string? RoleId { get; set; }
    public List<string>? Actions { get; set; }
}