using CoreLibrary.DataModels;

namespace Gateway.Models.Requests;

public class CreateBookRequest 
{
    public required Book Data { get; set; }
    public IFormFile? FileData { get; set; }
}