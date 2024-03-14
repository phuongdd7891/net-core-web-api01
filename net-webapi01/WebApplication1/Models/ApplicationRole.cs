using AspNetCore.Identity.MongoDbCore.Models;
using MongoDbGenericRepository.Attributes;
using System;
 
namespace WebApi.Models
{
    [CollectionName("Roles")]
    public class ApplicationRole : MongoIdentityRole<Guid>
    {
 
    }

    public class GetRolesReply
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
    }
}