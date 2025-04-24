namespace Gateway.Models.Response;

public class UserActionResponse
{
    public string? Method { get; set; }
    public string? ActionId { get; set; }
    public string? ControllerMethod { get; set; }
    public string? Description { get; set; }
    public string? Route { get; set; }
}