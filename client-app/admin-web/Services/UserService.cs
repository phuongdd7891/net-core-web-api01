﻿using AdminWeb.Models.Response;
using AdminWeb.Models;

namespace AdminWeb.Services
{
    public class UserService : BaseService
    {
        public UserService(
           HttpClient httpClient,
           IConfiguration configuration,
           IHttpContextAccessor contextAccessor
        ) : base(httpClient, configuration, contextAccessor)
        { }

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
            return GetAsync<ApiResponse<UsersResponse>>($"gw-api/user/list?skip={skip}&limit={limit}&customerId={customerId}");
        }

        public Task<ApiResponse<string>> SetLockUser(string username, bool isLock)
        {
            return PostAsync<Object, ApiResponse<string>>("gw-api/user/lock-user", new
            {
                Username = username,
                IsLock = isLock
            });
        }

        public Task<ApiResponse<UserViewModel>> GetUser(string username)
        {
            return GetAsync<ApiResponse<UserViewModel>>($"gw-api/user/get?username={username}");
        }

        public Task<ApiResponse<string>> UpdateUser(UserViewModel req)
        {
            return PostAsync<Object, ApiResponse<string>>("gw-api/user/update-user", req);
        }

        public Task<ApiResponse<string>> CreateUser(UserViewModel req)
        {
            return PostAsync<Object, ApiResponse<string>>("gw-api/user/create-user", req);
        }
    }
}
