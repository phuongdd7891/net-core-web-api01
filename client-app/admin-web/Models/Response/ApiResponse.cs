namespace AdminWeb.Models.Response
{
    public class ApiResponse<T>
    {
        public string Code { get; set; } = "Ok";
        public T? Data { get; set; }
    }
}
