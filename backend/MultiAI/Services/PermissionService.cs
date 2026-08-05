using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MultiAI.Authorization;
using MultiAI.Models.Data;
using MultiAI.Models.Entities;
using MultiAI.Services.Interface;

namespace MultiAI.Services
{
    public class PermissionService : IPermissionService
    {
        private readonly UserManager<Users> userManager;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly AppDbContext dbContext;

        public PermissionService(
            UserManager<Users> userManager,
            RoleManager<IdentityRole> roleManager,
            AppDbContext dbContext)
        {
            this.userManager = userManager;
            this.roleManager = roleManager;
            this.dbContext = dbContext;
        }

        public async Task<IReadOnlyList<string>> GetUserRolesAsync(Users user, CancellationToken cancellationToken = default)
        {
            var roles = await userManager.GetRolesAsync(user);
            return roles.OrderBy(x => x).ToList();
        }

        public async Task<IReadOnlyList<string>> GetUserPermissionsAsync(Users user, CancellationToken cancellationToken = default)
        {
            var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            var userClaims = await userManager.GetClaimsAsync(user);
            foreach (var claim in userClaims.Where(c =>
                         string.Equals(c.Type, AppClaimTypes.Permission, StringComparison.OrdinalIgnoreCase)))
            {
                if (!string.IsNullOrWhiteSpace(claim.Value))
                    permissions.Add(claim.Value);
            }

            var roles = await userManager.GetRolesAsync(user);
            foreach (var roleName in roles)
            {
                var role = await roleManager.FindByNameAsync(roleName);
                if (role == null)
                    continue;

                // Read from DB directly so audit-schema RoleClaims are visible even if Identity mapper is picky.
                var rolePermissionValues = await dbContext.Set<IdentityRoleClaim<string>>()
                    .AsNoTracking()
                    .Where(c => c.RoleId == role.Id && c.ClaimType == AppClaimTypes.Permission)
                    .Select(c => c.ClaimValue)
                    .ToListAsync(cancellationToken);

                foreach (var value in rolePermissionValues)
                {
                    if (!string.IsNullOrWhiteSpace(value))
                        permissions.Add(value!);
                }
            }

            return permissions.OrderBy(x => x).ToList();
        }

        public async Task EnsureDefaultRoleAsync(Users user, CancellationToken cancellationToken = default)
        {
            var roles = await userManager.GetRolesAsync(user);
            if (roles.Count > 0)
                return;

            await dbContext.Database.ExecuteSqlRawAsync("""
                IF NOT EXISTS (SELECT 1 FROM [Roles] WHERE [NormalizedName] = N'USER')
                BEGIN
                    INSERT INTO [Roles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp], [CreatedAt], [CreatedBy], [IsDeleted])
                    VALUES (NEWID(), N'User', N'USER', CONVERT(nvarchar(36), NEWID()), SYSUTCDATETIME(), N'System', 0);
                END
                """, cancellationToken);

            await dbContext.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO [UserRoles] ([UserId], [RoleId], [CreatedAt], [CreatedBy], [IsDeleted])
                SELECT {user.Id}, r.[Id], SYSUTCDATETIME(), N'System', 0
                FROM [Roles] r
                WHERE r.[NormalizedName] = N'USER'
                  AND NOT EXISTS (
                      SELECT 1 FROM [UserRoles] ur
                      WHERE ur.[UserId] = {user.Id} AND ur.[RoleId] = r.[Id]
                  );
                """, cancellationToken);
        }
    }
}
