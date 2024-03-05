namespace AdminWeb.Models.Response
{
    public class LoginResponse
    {
        public string Token { get; set; }
        public string Username { get; set; }
        public DateTime Expiration { get; set; }
    }
}
