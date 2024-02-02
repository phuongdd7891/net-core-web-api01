using System.Security.Claims;
using CoreLibrary.Repository;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;

namespace WebApi.Hubs
{
    [Authorize(AuthenticationSchemes = $"{JwtBearerDefaults.AuthenticationScheme}")]
    public class UserNotifications : Hub
    {
        private readonly RedisRepository _redisRepository;

        public UserNotifications(
            RedisRepository redisRepository
        )
        {
            _redisRepository = redisRepository;
        }

        public Task Send(string message)
        {
            return Task.CompletedTask;
        }

        public override Task OnConnectedAsync()
        {
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            return base.OnDisconnectedAsync(exception);
        }
    }
}