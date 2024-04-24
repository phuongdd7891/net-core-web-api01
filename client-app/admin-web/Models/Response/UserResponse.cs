namespace AdminWeb.Models.Response
{
    public class UsersResponse
    {
        public List<UserViewModel> List { get; set; }
        public int Total { get; set; }
    }

    public class UserRolesResponse
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? DisplayName { get; set; }
        public string[] Actions { get; set; } = new string[] {};
    }

    public class AdminProfile
    {
        public string? Id { get; set; }
        public required string Username { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public bool IsSystem { get; set; }
        public bool IsCustomer { get; set; }
        public bool Disabled { get; set; }
        public DateTime CreatedDate { get; set; }
        public int UserCount { get; set; }
        public string CreatedDateDisplay
        {
            get {
                return CreatedDate.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
            }
        }
    }
}
