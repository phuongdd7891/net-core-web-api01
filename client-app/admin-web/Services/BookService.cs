using AdminWeb.Models.Response;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace AdminWeb.Services
{
    public class BookService: BaseService
    {
        public BookService(
            HttpClient httpClient,
            IConfiguration configuration
        ): base(httpClient, configuration)
        { }

        public Task<ApiResponse<LoginResponse>> Login(string username, string password)
        {
            return PostAsync<Object, ApiResponse<LoginResponse>>("api/admin/login", new 
            {
                Username = username,
                Password = password
            });
        }

        public Task<ApiResponse<string>> Logout()
        {
            return PostAsync<Object, ApiResponse<string>>("api/operations/logout", null);
        }

        public Task<ApiResponse<string>> AddUserToRoles(string username, string[] roles)
        {
            return PostAsync<Object, ApiResponse<string>>("api/operations/add-user-roles", new
            {
                Username = username,
                Roles = roles
            });
        }

        public Task<ApiResponse<GetUsersReply>> GetUsers(int skip = 0, int limit = 100)
        {
            return GetAsync<ApiResponse<GetUsersReply>>($"api/operations/users?skip={skip}&limit={limit}");
        }

        public Task<ApiResponse<string>> SetLockUser(string username, bool isLock)
        {
            return PostAsync<Object, ApiResponse<string>>("api/operations/lock-user", new
            {
                Username = username,
                IsLock = isLock
            });
        }
    }
}
