using Grpc.Core;
using Userservice;

namespace WebApi.Services.Grpc;
public partial class UserGrpcService
{
    public override async Task<GetUserRolesReply> GetUserRoles(GetUserRolesRequest request, ServerCallContext context)
    {
        var result = new GetUserRolesReply();
        var loginUser = Helpers.GetClaimProfile(context);
        if (loginUser == null)
        {
            return result;
        }
        var roleActions = await _roleActionRepository.GetAll();
        var customerUsers = await _adminRepository.ListUsers(loginUser.IsCustomer);
        var data = _roleManager.Roles.Where(a => string.IsNullOrEmpty(a.CustomerId) || a.CustomerId == request.CustomerId || loginUser.IsSystem).ToList()
            .GroupJoin(roleActions, a => Convert.ToString(a.Id), x => x.RoleId, (a, x) => new { Role = a, Action = x })
            .GroupJoin(customerUsers, a => a.Role.CustomerId, x => x.Id, (a, x) => new { a.Role, RoleAct = a.Action, Customers = x })
            .SelectMany(a => a.Customers.DefaultIfEmpty(), (a, x) =>
            {
                var role = new UserRole
                {
                    Id = Convert.ToString(a.Role.Id),
                    Name = a.Role.Name,
                    DisplayName = GetRoleDisplayName(a.Role.Name ?? string.Empty, a.Role.CustomerId),
                    CustomerId = a.Role.CustomerId,
                    CustomerName = x?.FullName ?? string.Empty
                };
                role.Actions.AddRange(a.RoleAct.SelectMany(x => x.Actions ?? new List<string>()));
                return role;
            }).ToList();
        result.Data.AddRange(data);
        return result;
    }

    private string GetRoleDisplayName(string name, string? customerId)
    {
        var roleNameArr = name!.Split("__", StringSplitOptions.RemoveEmptyEntries);
        return string.IsNullOrEmpty(customerId) ? name : string.Join("", roleNameArr, roleNameArr.Length > 1 ? roleNameArr.Length - 1 : roleNameArr.Length, 1);
    }
}