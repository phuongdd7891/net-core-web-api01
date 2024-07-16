using System.Security.Principal;

namespace WebApi.Authentication
{
    public class UserPrincipal : GenericPrincipal
    {
        public new UserIdentity Identity
        {
            get; private set;
        }

        public UserPrincipal(UserIdentity userIdentity) : base(userIdentity, userIdentity.UserRoles)
        {
            Identity = userIdentity;
        }
    }

    public static class HttpRequestExtension
    {
        public static UserIdentity User(this HttpContext context)
        {
            UserIdentity? user = null;

            var identity = (context.User as UserPrincipal);
            if (identity != null)
            {
                user = identity?.Identity;
            }

            return user;
        }
    }
}