namespace AdminWeb.Models.Response
{
    public class GetUsersResponse
    {
        public List<UserViewModel> List { get; set; }
        public int Total { get; set; }
    }

    public class GetUserRolesResponse
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
    }

    public class AdminProfile
    {
        public string? Id { get; set; }
        public required string Username { get; set; }
        public bool IsSystem { get; set; }
        public bool IsCustomer { get; set; }
    }
}
