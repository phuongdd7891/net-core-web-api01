
namespace WebApi.Models.Requests;

public class CreateBookRequest 
{
    public Book Data { get; set; }
    public IFormFile? FileData { get; set; }
}