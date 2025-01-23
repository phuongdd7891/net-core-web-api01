namespace AdminMicroService.Models;

public class GetRolesReply
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? DisplayName
    {
        get
        {
            var roleNameArr = Name!.Split("__", StringSplitOptions.RemoveEmptyEntries);
            return string.IsNullOrEmpty(CustomerId) ? Name : string.Join("", roleNameArr, roleNameArr.Length > 1 ? roleNameArr.Length - 1 : roleNameArr.Length, 1);
        }
    }
    public List<string>? Actions { get; set; }
    public string? CustomerId { get; set; }
    public string? CustomerName { get; set; }
}