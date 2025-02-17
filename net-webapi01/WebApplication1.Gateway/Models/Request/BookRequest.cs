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