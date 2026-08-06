using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MultiAI.Authorization;
using MultiAI.Models.Data;
using MultiAI.Models.DTOs;
using MultiAI.Models.Entities;
using MultiAI.Services.Interface;

namespace MultiAI.Services
{
    public class AdminRoleService : IAdminRoleService
    {
        private static readonly HashSet<string> SystemRoles = new(StringComparer.OrdinalIgnoreCase)
        {
            AppRoles.Admin,
            AppRoles.Moderator,
            AppRoles.User,
        };

        private readonly RoleManager<IdentityRole> roleManager;
        private readonly AppDbContext db;

        public AdminRoleService(RoleManager<IdentityRole> roleManager, AppDbContext db)
        {
            this.roleManager = roleManager;
            this.db = db;
        }

        public Task<IReadOnlyList<string>> GetAllPermissionsAsync()
            => Task.FromResult<IReadOnlyList<string>>(AppPermissions.All.OrderBy(x => x).ToList());

        public async Task<List<RoleListItemDto>> GetRolesAsync(CancellationToken cancellationToken = default)
        {
            var roles = await roleManager.Roles
                .AsNoTracking()
                .OrderBy(r => r.Name)
                .ToListAsync(cancellationToken);

            var result = new List<RoleListItemDto>();
            foreach (var role in roles)
            {
                if (await IsSoftDeletedAsync(role.Id, cancellationToken))
                    continue;

                result.Add(await ToDtoAsync(role, cancellationToken));
            }

            return result;
        }

        public async Task<RoleListItemDto?> GetRoleAsync(string roleId, CancellationToken cancellationToken = default)
        {
            var role = await roleManager.FindByIdAsync(roleId);
            if (role == null || await IsSoftDeletedAsync(role.Id, cancellationToken))
                return null;

            return await ToDtoAsync(role, cancellationToken);
        }

        public async Task<RoleListItemDto> CreateRoleAsync(
            CreateRoleDto dto,
            string actorUserId,
            CancellationToken cancellationToken = default)
        {
            var name = (dto.Name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException("Role name is required.");

            if (SystemRoles.Contains(name))
                throw new InvalidOperationException("Cannot recreate a system role with that name.");

            if (await roleManager.RoleExistsAsync(name))
                throw new InvalidOperationException("A role with this name already exists.");

            var permissions = NormalizePermissions(dto.Permissions);
            var roleId = Guid.NewGuid().ToString();
            var normalized = name.ToUpperInvariant();
            var stamp = Guid.NewGuid().ToString();

            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO [Roles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp], [CreatedAt], [CreatedBy], [IsDeleted])
                VALUES ({roleId}, {name}, {normalized}, {stamp}, SYSUTCDATETIME(), {actorUserId}, 0);
                """, cancellationToken);

            await ReplacePermissionsAsync(roleId, permissions, actorUserId, cancellationToken);

            var role = await roleManager.FindByIdAsync(roleId)
                ?? throw new InvalidOperationException("Role was created but could not be loaded.");

            return await ToDtoAsync(role, cancellationToken);
        }

        public async Task<RoleListItemDto> UpdateRoleAsync(
            string roleId,
            UpdateRoleDto dto,
            string actorUserId,
            CancellationToken cancellationToken = default)
        {
            var role = await roleManager.FindByIdAsync(roleId)
                ?? throw new KeyNotFoundException("Role not found.");

            if (await IsSoftDeletedAsync(role.Id, cancellationToken))
                throw new KeyNotFoundException("Role not found.");

            var name = (dto.Name ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException("Role name is required.");

            var isSystem = SystemRoles.Contains(role.Name ?? string.Empty);
            if (isSystem && !string.Equals(role.Name, name, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("System roles cannot be renamed.");

            if (!string.Equals(role.Name, name, StringComparison.OrdinalIgnoreCase)
                && await roleManager.RoleExistsAsync(name))
                throw new InvalidOperationException("A role with this name already exists.");

            if (!isSystem)
            {
                role.Name = name;
                role.NormalizedName = name.ToUpperInvariant();
                var update = await roleManager.UpdateAsync(role);
                if (!update.Succeeded)
                    throw new InvalidOperationException(string.Join("; ", update.Errors.Select(e => e.Description)));

                await db.Database.ExecuteSqlInterpolatedAsync($"""
                    UPDATE [Roles]
                    SET [UpdatedAt] = SYSUTCDATETIME(), [UpdatedBy] = {actorUserId}
                    WHERE [Id] = {roleId};
                    """, cancellationToken);
            }

            var permissions = NormalizePermissions(dto.Permissions);
            if (string.Equals(role.Name, AppRoles.Admin, StringComparison.OrdinalIgnoreCase))
            {
                // Admin always keeps every known permission.
                permissions = AppPermissions.All.ToList();
            }

            await ReplacePermissionsAsync(role.Id, permissions, actorUserId, cancellationToken);

            role = (await roleManager.FindByIdAsync(roleId))!;
            return await ToDtoAsync(role, cancellationToken);
        }

        public async Task DeleteRoleAsync(
            string roleId,
            string actorUserId,
            CancellationToken cancellationToken = default)
        {
            var role = await roleManager.FindByIdAsync(roleId)
                ?? throw new KeyNotFoundException("Role not found.");

            if (SystemRoles.Contains(role.Name ?? string.Empty))
                throw new InvalidOperationException("System roles cannot be deleted.");

            var assignedUsers = await db.Set<UserRoles>()
                .AsNoTracking()
                .CountAsync(ur => ur.RoleId == roleId && !ur.IsDeleted, cancellationToken);

            if (assignedUsers > 0)
                throw new InvalidOperationException("Cannot delete a role that is still assigned to users.");

            await db.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE [Roles]
                SET [IsDeleted] = 1,
                    [DeletedAt] = SYSUTCDATETIME(),
                    [DeletedBy] = {actorUserId},
                    [UpdatedAt] = SYSUTCDATETIME(),
                    [UpdatedBy] = {actorUserId}
                WHERE [Id] = {roleId};
                """, cancellationToken);
        }

        private async Task ReplacePermissionsAsync(
            string roleId,
            IReadOnlyList<string> permissions,
            string actorUserId,
            CancellationToken cancellationToken)
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                DELETE FROM [RoleClaims]
                WHERE [RoleId] = {roleId} AND [ClaimType] = {AppClaimTypes.Permission};
                """, cancellationToken);

            foreach (var permission in permissions)
            {
                await db.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO [RoleClaims] ([RoleId], [ClaimType], [ClaimValue], [CreatedAt], [CreatedBy], [IsDeleted])
                    VALUES ({roleId}, {AppClaimTypes.Permission}, {permission}, SYSUTCDATETIME(), {actorUserId}, 0);
                    """, cancellationToken);
            }
        }

        private static List<string> NormalizePermissions(IEnumerable<string>? permissions)
        {
            var allowed = new HashSet<string>(AppPermissions.All, StringComparer.OrdinalIgnoreCase);
            return (permissions ?? [])
                .Where(p => !string.IsNullOrWhiteSpace(p) && allowed.Contains(p.Trim()))
                .Select(p => allowed.First(a => string.Equals(a, p.Trim(), StringComparison.OrdinalIgnoreCase)))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(p => p)
                .ToList();
        }

        private async Task<bool> IsSoftDeletedAsync(string roleId, CancellationToken cancellationToken)
        {
            var flag = await db.Database
                .SqlQueryRaw<int>("SELECT CAST([IsDeleted] AS int) AS [Value] FROM [Roles] WHERE [Id] = {0}", roleId)
                .FirstOrDefaultAsync(cancellationToken);
            return flag == 1;
        }

        private async Task<RoleListItemDto> ToDtoAsync(IdentityRole role, CancellationToken cancellationToken)
        {
            var permissions = await db.Set<IdentityRoleClaim<string>>()
                .AsNoTracking()
                .Where(c => c.RoleId == role.Id && c.ClaimType == AppClaimTypes.Permission)
                .Select(c => c.ClaimValue!)
                .Where(v => v != null && v != string.Empty)
                .OrderBy(v => v)
                .ToListAsync(cancellationToken);

            var userCount = await db.Set<UserRoles>()
                .AsNoTracking()
                .CountAsync(ur => ur.RoleId == role.Id && !ur.IsDeleted, cancellationToken);

            return new RoleListItemDto
            {
                Id = role.Id,
                Name = role.Name ?? string.Empty,
                UserCount = userCount,
                Permissions = permissions,
            };
        }
    }
}
