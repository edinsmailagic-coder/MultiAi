using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MultiAI.Authorization;
using MultiAI.Models.Data;
using MultiAI.Models.DTOs;
using MultiAI.Models.Entities;
using MultiAI.Services.Interface;

namespace MultiAI.Services
{
    public class AdminUserService : IAdminUserService
    {
        private readonly UserManager<Users> userManager;
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly AppDbContext db;
        private readonly IPermissionService permissionService;

        public AdminUserService(
            UserManager<Users> userManager,
            RoleManager<IdentityRole> roleManager,
            AppDbContext db,
            IPermissionService permissionService)
        {
            this.userManager = userManager;
            this.roleManager = roleManager;
            this.db = db;
            this.permissionService = permissionService;
        }

        public async Task<List<AdminUserListItemDto>> GetUsersAsync(CancellationToken cancellationToken = default)
        {
            var users = await userManager.Users
                .AsNoTracking()
                .OrderBy(u => u.Email)
                .ToListAsync(cancellationToken);

            var result = new List<AdminUserListItemDto>(users.Count);
            foreach (var user in users)
                result.Add(await ToListItemAsync(user, cancellationToken));

            return result;
        }

        public async Task<AdminUserDetailDto?> GetUserAsync(string userId, CancellationToken cancellationToken = default)
        {
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
                return null;

            var item = await ToListItemAsync(user, cancellationToken);
            var permissions = await permissionService.GetUserPermissionsAsync(user, cancellationToken);

            return new AdminUserDetailDto
            {
                Id = item.Id,
                Email = item.Email,
                UserName = item.UserName,
                FirstName = item.FirstName,
                LastName = item.LastName,
                IsDeleted = item.IsDeleted,
                LockoutEnabled = item.LockoutEnabled,
                IsLockedOut = item.IsLockedOut,
                CreatedAt = item.CreatedAt,
                Roles = item.Roles,
                Permissions = permissions.ToList(),
            };
        }

        public async Task<AdminUserDetailDto> CreateUserAsync(
            CreateAdminUserDto dto,
            string actorUserId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
                throw new InvalidOperationException("Email and password are required.");

            var email = dto.Email.Trim();
            var existing = await userManager.FindByEmailAsync(email);
            if (existing != null)
                throw new InvalidOperationException("A user with this email already exists.");

            var roles = await NormalizeAndValidateRolesAsync(dto.Roles, cancellationToken);
            if (roles.Count == 0)
                roles.Add(AppRoles.User);

            var actor = await userManager.FindByIdAsync(actorUserId);
            var createdBy = actor?.UserName ?? actorUserId;

            var user = new Users
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = dto.FirstName?.Trim() ?? string.Empty,
                LastName = dto.LastName?.Trim() ?? string.Empty,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy,
                LockoutEnabled = true,
            };

            var createResult = await userManager.CreateAsync(user, dto.Password);
            if (!createResult.Succeeded)
                throw new InvalidOperationException(string.Join("; ", createResult.Errors.Select(e => e.Description)));

            await ReplaceUserRolesAsync(user.Id, roles, createdBy, cancellationToken);

            var detail = await GetUserAsync(user.Id, cancellationToken);
            return detail!;
        }

        public async Task<AdminUserDetailDto> UpdateUserAsync(
            string userId,
            UpdateAdminUserDto dto,
            string actorUserId,
            CancellationToken cancellationToken = default)
        {
            var user = await userManager.FindByIdAsync(userId)
                ?? throw new KeyNotFoundException("User not found.");

            var actor = await userManager.FindByIdAsync(actorUserId);
            var updatedBy = actor?.UserName ?? actorUserId;

            if (!string.IsNullOrWhiteSpace(dto.FirstName))
                user.FirstName = dto.FirstName.Trim();
            if (!string.IsNullOrWhiteSpace(dto.LastName))
                user.LastName = dto.LastName.Trim();

            if (!string.IsNullOrWhiteSpace(dto.Email))
            {
                var email = dto.Email.Trim();
                if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
                {
                    var other = await userManager.FindByEmailAsync(email);
                    if (other != null && other.Id != user.Id)
                        throw new InvalidOperationException("A user with this email already exists.");

                    user.Email = email;
                    user.UserName = email;
                    user.NormalizedEmail = userManager.NormalizeEmail(email);
                    user.NormalizedUserName = userManager.NormalizeName(email);
                }
            }

            if (dto.IsActive.HasValue)
            {
                if (dto.IsActive.Value)
                {
                    user.IsDeleted = false;
                    user.DeletedAt = null;
                    user.DeletedBy = null;
                    user.LockoutEnd = null;
                }
                else
                {
                    if (string.Equals(userId, actorUserId, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("You cannot deactivate your own account.");

                    await EnsureNotLastAdminAsync(user, cancellationToken);
                    user.IsDeleted = true;
                    user.DeletedAt = DateTime.UtcNow;
                    user.DeletedBy = updatedBy;
                    user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100);
                }
            }

            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedBy = updatedBy;

            var updateResult = await userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
                throw new InvalidOperationException(string.Join("; ", updateResult.Errors.Select(e => e.Description)));

            if (dto.Roles != null)
            {
                var roles = await NormalizeAndValidateRolesAsync(dto.Roles, cancellationToken);
                if (roles.Count == 0)
                    throw new InvalidOperationException("User must have at least one role.");

                if (string.Equals(userId, actorUserId, StringComparison.OrdinalIgnoreCase))
                {
                    var currentRoles = await permissionService.GetUserRolesAsync(user, cancellationToken);
                    var wasAdmin = currentRoles.Any(r => string.Equals(r, AppRoles.Admin, StringComparison.OrdinalIgnoreCase));
                    var staysAdmin = roles.Any(r => string.Equals(r, AppRoles.Admin, StringComparison.OrdinalIgnoreCase));
                    if (wasAdmin && !staysAdmin)
                        throw new InvalidOperationException("You cannot remove your own Admin role.");
                }

                await ReplaceUserRolesAsync(user.Id, roles, updatedBy, cancellationToken);
            }

            var detail = await GetUserAsync(user.Id, cancellationToken);
            return detail!;
        }

        public async Task SoftDeleteUserAsync(
            string userId,
            string actorUserId,
            CancellationToken cancellationToken = default)
        {
            await UpdateUserAsync(
                userId,
                new UpdateAdminUserDto { IsActive = false },
                actorUserId,
                cancellationToken);
        }

        public async Task ResetPasswordAsync(
            string userId,
            string newPassword,
            string actorUserId,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(newPassword))
                throw new InvalidOperationException("Password is required.");

            var user = await userManager.FindByIdAsync(userId)
                ?? throw new KeyNotFoundException("User not found.");

            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var result = await userManager.ResetPasswordAsync(user, token, newPassword);
            if (!result.Succeeded)
                throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));

            var actor = await userManager.FindByIdAsync(actorUserId);
            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedBy = actor?.UserName ?? actorUserId;
            await userManager.UpdateAsync(user);
        }

        private async Task EnsureNotLastAdminAsync(Users user, CancellationToken cancellationToken)
        {
            var roles = await permissionService.GetUserRolesAsync(user, cancellationToken);
            if (!roles.Any(r => string.Equals(r, AppRoles.Admin, StringComparison.OrdinalIgnoreCase)))
                return;

            var adminRole = await roleManager.FindByNameAsync(AppRoles.Admin);
            if (adminRole == null)
                return;

            var otherActiveAdmins = await (
                from ur in db.Set<UserRoles>().AsNoTracking()
                join u in userManager.Users.AsNoTracking() on ur.UserId equals u.Id
                where ur.RoleId == adminRole.Id
                      && !ur.IsDeleted
                      && !u.IsDeleted
                      && u.Id != user.Id
                select u.Id
            ).CountAsync(cancellationToken);

            if (otherActiveAdmins == 0)
                throw new InvalidOperationException("Cannot deactivate the last active Admin.");
        }

        private async Task<List<string>> NormalizeAndValidateRolesAsync(
            IEnumerable<string>? roles,
            CancellationToken cancellationToken)
        {
            var names = (roles ?? [])
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Select(r => r.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var validated = new List<string>();
            foreach (var name in names)
            {
                var role = await roleManager.FindByNameAsync(name);
                if (role == null || await IsRoleSoftDeletedAsync(role.Id, cancellationToken))
                    throw new InvalidOperationException($"Role '{name}' was not found.");

                validated.Add(role.Name!);
            }

            return validated;
        }

        private async Task<bool> IsRoleSoftDeletedAsync(string roleId, CancellationToken cancellationToken)
        {
            var flag = await db.Database
                .SqlQueryRaw<int>("SELECT CAST([IsDeleted] AS int) AS [Value] FROM [Roles] WHERE [Id] = {0}", roleId)
                .FirstOrDefaultAsync(cancellationToken);
            return flag == 1;
        }

        private async Task ReplaceUserRolesAsync(
            string userId,
            IReadOnlyList<string> roleNames,
            string actorName,
            CancellationToken cancellationToken)
        {
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                DELETE FROM [UserRoles] WHERE [UserId] = {userId};
                """, cancellationToken);

            foreach (var roleName in roleNames)
            {
                var role = await roleManager.FindByNameAsync(roleName);
                if (role == null)
                    continue;

                await db.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO [UserRoles] ([UserId], [RoleId], [CreatedAt], [CreatedBy], [IsDeleted])
                    VALUES ({userId}, {role.Id}, SYSUTCDATETIME(), {actorName}, 0);
                    """, cancellationToken);
            }
        }

        private async Task<AdminUserListItemDto> ToListItemAsync(Users user, CancellationToken cancellationToken)
        {
            var roles = await permissionService.GetUserRolesAsync(user, cancellationToken);
            var locked = user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow;

            return new AdminUserListItemDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                UserName = user.UserName ?? string.Empty,
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                IsDeleted = user.IsDeleted,
                LockoutEnabled = user.LockoutEnabled,
                IsLockedOut = locked || user.IsDeleted,
                CreatedAt = user.CreatedAt,
                Roles = roles.ToList(),
            };
        }
    }
}
