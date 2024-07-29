using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.AspNetCore.Identity;
using MongoDB.Driver.Linq;
using UserMicroservice.Protos;
using WebApi.Models;

namespace WebApi.Services;

public class UserService : UserServiceProto.UserServiceProtoBase
{
    private UserManager<ApplicationUser> _userManager;

    public UserService(
        UserManager<ApplicationUser> userManager
    )
    {
        _userManager = userManager;
    }

    public override Task<UserCountReply> CountUsers(CountUserByCustomerRequest request, ServerCallContext context)
    {
        var result = new UserCountReply();
        if (!string.IsNullOrEmpty(request.CustomerId))
        {
            var count = _userManager.Users.Count(a => a.CustomerId == request.CustomerId);
            var list = new[] {
                new UserCount {
                    CustomerId = request.CustomerId,
                    Count = count
                }
            };
            result.List.AddRange(list);
        }
        else
        {
            var customerIds = _userManager.Users.Where(a => !string.IsNullOrEmpty(a.CustomerId)).Select(a => a.CustomerId).Distinct().ToList();
            customerIds.ForEach(x =>
            {
                var count = _userManager.Users.Count(a => a.CustomerId == x);
                result.List.Add(new UserCount
                {
                    CustomerId = x,
                    Count = count
                });
            });
        }
        return Task.FromResult(result);
    }

    public override Task<UsersReply> GetUsers(Empty request, ServerCallContext context)
    {
        var result = new UsersReply();
        var users = _userManager.Users;
        result.List.AddRange(users.AsEnumerable().Select(a =>
        {
            var user = new User
            {
                Id = a.Id.ToString(),
                UserName = a.UserName,
                CustomerId = a.CustomerId ?? string.Empty,
                Email = a.Email
            };
            user.Roles.AddRange(a.Roles.Select(b => b.ToString()));
            return user;
        }));
        return Task.FromResult(result);
    }
}