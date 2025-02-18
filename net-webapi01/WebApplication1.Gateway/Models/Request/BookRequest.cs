using CoreLibrary.DataModels;

namespace Gateway.Models.Requests;

public class CreateBookRequest 
{
    public required Book Data { get; set; }
    public IFormFile? FileData { get; set; }
}

public class UpdateBookRequest 
{
    public string Id { get; set; }
    public required Book Data { get; set; }
    public IFormFile? FileData { get; set; }
}

public class CloneBookRequest
{
    public string Id { get; set; }
    public int Quantity { get; set; }
}

public class DeleteCloneBooksRequest
{
    public string Id { get; set; }
    public int From { get; set; }
    public int To { get; set; }
}

public class GetBooksRequest
{
    public string? SearchKey { get; set; }
    public bool SearchExact { get; set; } = false;
    public string? CreatedFrom { get; set; }
    public string? CreatedTo { get; set; }
    public int Skip { get; set; }
    public int Limit { get; set; }
}