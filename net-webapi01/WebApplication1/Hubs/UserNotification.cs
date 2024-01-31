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
            if (!string.IsNullOrEmpty(message))
            {
                try
                {
                    var obj = JsonConvert.DeserializeObject<dynamic>(message);
                    if (obj?["connectionId"] != null)
                    {
                        var identity = Context.User!.Identity as ClaimsIdentity;
                        _redisRepository.Set(identity!.FindFirst(ClaimTypes.Name)!.Value, obj["connectionId"].Value, 24 * 60);
                    }
                }
                finally { }
            }
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