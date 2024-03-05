namespace AdminWeb.Models.Response
{
    public class GetUsersReply
    {
        public List<UserViewModel> List { get; set; }
        public int Total { get; set; }
    }
}
