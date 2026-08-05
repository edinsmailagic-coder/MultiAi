using MultiAI.Authorization;

namespace MultiAI.Config
{
    public static class AuthorizationConfig
    {
        public static IServiceCollection AddPermissionAuthorization(this IServiceCollection services)
        {
            services.AddAuthorization(options =>
            {
                foreach (var permission in AppPermissions.All)
                {
                    options.AddPolicy(permission, policy =>
                        policy.RequireAuthenticatedUser()
                              .RequireClaim(AppClaimTypes.Permission, permission));
                }
            });

            return services;
        }
    }
}
