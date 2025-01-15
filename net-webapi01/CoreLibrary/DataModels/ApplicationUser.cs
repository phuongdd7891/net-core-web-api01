using AspNetCore.Identity.MongoDbCore.Models;
using MongoDB.Bson.Serialization.Attributes;
using MongoDbGenericRepository.Attributes;
using System;
using System.Text.Json.Serialization;

namespace CoreLibrary.DataModels
{
    [CollectionName("Users")]
    public class ApplicationUser : MongoIdentityUser<Guid>
    {
        public string? CustomerId { get; set; }
    }
}