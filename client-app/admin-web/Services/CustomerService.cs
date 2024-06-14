using AdminWeb.Models.Response;
using AdminWeb.Models;

namespace AdminWeb.Services
{
    public class CustomerService: BaseService
    {

        public CustomerService(
            HttpClient httpClient,
            IConfiguration configuration): base(httpClient, configuration) { }

        public Task<ApiResponse<List<AdminProfile>>> GetCustomers()
        {
            return GetAsync<ApiResponse<List<AdminProfile>>>("api/admin/customer-users");
        }

        public Task<ApiResponse<CustomerViewModel>> GetCustomer(string username)
        {
            return GetAsync<ApiResponse<CustomerViewModel>>($"api/admin/get-user?username={username}");
        }

        public Task<ApiResponse<string>> CreateCustomer(CustomerViewModel req)
        {
            return PostAsync<Object, ApiResponse<string>>("api/admin/create-user", new
            {
                Username = req.Username,
                Password = req.Password,
                FullName = req.FullName,
                Email = req.Email,
                IsCustomer = true
            });
        }

        public Task<ApiResponse<string>> UpdateCustomer(CustomerViewModel req)
        {
            return PostAsync<Object, ApiResponse<string>>("api/admin/update-user", new
            {
                Username = req.Username,
                Password = req.Password,
                FullName = req.FullName,
                Email = req.Email,
                Disabled = req.Disabled,
            });
        }
    }
}
