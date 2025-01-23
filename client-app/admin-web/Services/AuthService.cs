using AdminWeb.Models.Response;

namespace AdminWeb.Services
{
    public class AuthService : BaseService
    {
        private readonly IHttpContextAccessor _contextAccessor;

        public AuthService(
            HttpClient httpClient,
            IConfiguration configuration,
            IHttpContextAccessor contextAccessor
        ) : base(httpClient, configuration, contextAccessor)
        {
            _contextAccessor = contextAccessor;
        }

        public Task<ApiResponse<LoginResponse>> Login(string username, string password)
        {
            return PostAsync<Object, ApiResponse<LoginResponse>>("gw-api/admin/login", new
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

        public Task AddRoleActions(string roleId, string[] actions)
        {
            return PostAsync<Object, string?>("gw-api/user/add-role-actions", new {
                RoleId = roleId ,
                Actions = actions
            });
        }

        public Task<ApiResponse<string>> CreateRole(string name, string? customerId)
        {
            return PostAsync<object, ApiResponse<string>>("gw-api/user/create-role", new
            {
                Name = name,
                CustomerId = customerId
            });
        }

        public Task EditRole(string id, string name, string? customerId)
        {
            return PostAsync<object, string?>("gw-api/user/edit-role", new
            {
                Id = id,
                Name = name,
                CustomerId = customerId
            });
        }

        public Task DeleteRole(string roleId)
        {
            return PostAsync<Object, string?>("api/operations/delete-role", roleId);
        }

        public Task<ApiResponse<UserRolesResponse[]>> GetUserRoles(string? customerId = null)
        {
            return GetAsync<ApiResponse<UserRolesResponse[]>>($"gw-api/user/user-roles?customerId={customerId}");
        }

        public Task<ApiResponse<List<UserActionResponse>>> GetUserActions()
        {
            return GetAsync<ApiResponse<List<UserActionResponse>>>($"gw-api/user/user-actions");
        }

        public Task ChangePassword(string currentPassword, string newPassword)
        {
            return PostAsync<Object, string?>("api/admin/change-password", new
            {
                CurrentPassword = currentPassword,
                NewPassword = newPassword
            });
        }
    }
}
