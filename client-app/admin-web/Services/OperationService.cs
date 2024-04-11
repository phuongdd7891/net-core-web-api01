using AdminWeb.Models;
using AdminWeb.Models.Response;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace AdminWeb.Services
{
    public class OperationService : BaseService
    {
        private readonly IHttpContextAccessor _contextAccessor;

        public OperationService(
            HttpClient httpClient,
            IConfiguration configuration,
            IHttpContextAccessor contextAccessor
        ) : base(httpClient, configuration)
        {
            _contextAccessor = contextAccessor;
        }

        public Task<ApiResponse<LoginResponse>> Login(string username, string password)
        {
            return PostAsync<Object, ApiResponse<LoginResponse>>("api/admin/login", new
            {
                Username = username,
                Password = password
            });
        }

        public Task<ApiResponse<string>?> Logout()
        {
            return PostAsync<Object, ApiResponse<string>>("api/operations/logout", null).ContinueWith(res =>
            {
                _contextAccessor.HttpContext!.Response.Cookies.Delete(Const.AuthenticationKey);
                return res.IsFaulted ? null : res.Result;
            });
        }

        public Task<ApiResponse<string>> AddUserToRoles(string username, string[] roles)
        {
            return PostAsync<Object, ApiResponse<string>>("api/operations/add-user-roles", new
            {
                Username = username,
                Roles = roles
            });
        }

        public Task<ApiResponse<UsersResponse>> GetUsers(int skip = 0, int limit = 100, string? customerId = null)
        {
            return GetAsync<ApiResponse<UsersResponse>>($"api/operations/users?skip={skip}&limit={limit}&customerId={customerId}");
        }

        public Task<ApiResponse<string>> SetLockUser(string username, bool isLock)
        {
            return PostAsync<Object, ApiResponse<string>>("api/operations/lock-user", new
            {
                Username = username,
                IsLock = isLock
            });
        }

        public Task<ApiResponse<UserRolesResponse[]>> GetUserRoles()
        {
            return GetAsync<ApiResponse<UserRolesResponse[]>>("api/operations/user-roles");
        }

        public Task<ApiResponse<AdminProfile>> GetProfile()
        {
            return GetAsync<ApiResponse<AdminProfile>>("api/admin/user-profile");
        }

        public Task<ApiResponse<UserViewModel>> GetUser(string username)
        {
            return GetAsync<ApiResponse<UserViewModel>>($"api/operations/user?username={username}");
        }

        public Task<ApiResponse<string>> UpdateUser(UserViewModel req)
        {
            return PostAsync<Object, ApiResponse<string>>("api/operations/update-user", req);
        }

        public Task<ApiResponse<string>> CreateUser(UserViewModel req)
        {
            return PostAsync<Object, ApiResponse<string>>("api/operations/create-user", req);
        }

        #region Customer
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
        #endregion
    }
}
