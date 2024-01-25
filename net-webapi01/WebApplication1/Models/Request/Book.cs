
namespace WebApi.Models.Requests;

public class CreateBookRequest 
{
    public required Book Data { get; set; }
    public IFormFile? FileData { get; set; }
}

public class CreateBookCateogryRequest
{
    public string? Id { get; set; }
    public required string Name { get; set; }
    public string? Parent { get; set; }
}

public class GetBooksReply
{
    public List<Book>? List { get; set; }
    public long Total { get; set; }
}