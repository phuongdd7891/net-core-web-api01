using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace WebApi.Models;

public class Book
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("Name")]
    [Newtonsoft.Json.JsonProperty("name")]
    public string? BookName { get; set; }

    public decimal Price { get; set; }

    public string? Category { get; set; }

    public string? Author { get; set; }
    public string? CoverPicture { get; set; }

    public DateTime? CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public string? CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }
}

public class BookCategory
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("Name")]
    [Newtonsoft.Json.JsonProperty("name")]
    public string? CategoryName { get; set; }
    public string? ParentPath { get; set; }
}