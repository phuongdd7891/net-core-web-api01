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
        public string? DisplayName { get; set; }
        public List<string>? Actions { get; set; }
    }

    public class UserActionResponse
    {
        public string? Method { get; set; }
        public string? Action { get; set; }
        public string? ControllerMethod { get; set; }
        public string? DisplayName { get; set; }
    }
}