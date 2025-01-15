using AspNetCore.Identity.MongoDbCore.Models;
using MongoDbGenericRepository.Attributes;
 
namespace CoreLibrary.DataModels
{
    [CollectionName("Roles")]
    public class ApplicationRole : MongoIdentityRole<Guid>
    {
        public string? CustomerId { get; set; }
    }
}