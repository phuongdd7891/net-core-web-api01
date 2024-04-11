namespace AdminWeb.Models
{
    public class CustomerViewModel
    {
        public string? Id { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public bool Disabled { get; set; }
    }
}
