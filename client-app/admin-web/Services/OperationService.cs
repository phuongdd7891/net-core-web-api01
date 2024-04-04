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

        public Task<ApiResponse<GetUsersResponse>> GetUsers(int skip = 0, int limit = 100)
        {
            return GetAsync<ApiResponse<GetUsersResponse>>($"api/operations/users?skip={skip}&limit={limit}");
        }

        public Task<ApiResponse<string>> SetLockUser(string username, bool isLock)
        {
            return PostAsync<Object, ApiResponse<string>>("api/operations/lock-user", new
            {
                Username = username,
                IsLock = isLock
            });
        }

        public Task<ApiResponse<GetUserRolesResponse>> GetUserRoles()
        {
            return GetAsync<ApiResponse<GetUserRolesResponse>>("api/operations/user-roles");
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
    }
}
