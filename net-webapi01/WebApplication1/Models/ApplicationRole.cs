using AspNetCore.Identity.MongoDbCore.Models;
using MongoDbGenericRepository.Attributes;
using System;
 
namespace WebApi.Models
{
    [CollectionName("Roles")]
    public class ApplicationRole : MongoIdentityRole<Guid>
    {
        public string? CustomerId { get; set; }
    }

    public class GetRolesReply
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? DisplayName
        {
            get
            {
                var roleNameArr = Name!.Split("__", StringSplitOptions.RemoveEmptyEntries);
                return string.IsNullOrEmpty(CustomerId) ? Name : string.Join("", roleNameArr, roleNameArr.Length > 1 ? 1 : 0, roleNameArr.Length > 1 ? roleNameArr.Length - 1 : roleNameArr.Length);
            }
        }
        public List<string>? Actions { get; set; }
        public string? CustomerId { get; set; }
        public string? CustomerName { get; set; }
    }

    public class UserActionResponse
    {
        public string? Method { get; set; }
        public string? Action { get; set; }
        public string? ControllerMethod { get; set; }
        public string? Description { get; set; }
    }
}