using MultiAI.Models.DTOs;
using MultiAI.Models.Entities;

namespace MultiAI.Services.Interface
{
    public interface IAdminUserService
    {
        Task<List<AdminUserListItemDto>> GetUsersAsync(CancellationToken cancellationToken = default);
        Task<AdminUserDetailDto?> GetUserAsync(string userId, CancellationToken cancellationToken = default);
        Task<AdminUserDetailDto> CreateUserAsync(CreateAdminUserDto dto, string actorUserId, CancellationToken cancellationToken = default);
        Task<AdminUserDetailDto> UpdateUserAsync(string userId, UpdateAdminUserDto dto, string actorUserId, CancellationToken cancellationToken = default);
        Task SoftDeleteUserAsync(string userId, string actorUserId, CancellationToken cancellationToken = default);
        Task ResetPasswordAsync(string userId, string newPassword, string actorUserId, CancellationToken cancellationToken = default);
    }

    public interface IAdminRoleService
    {
        Task<List<RoleListItemDto>> GetRolesAsync(CancellationToken cancellationToken = default);
        Task<RoleListItemDto?> GetRoleAsync(string roleId, CancellationToken cancellationToken = default);
        Task<RoleListItemDto> CreateRoleAsync(CreateRoleDto dto, string actorUserId, CancellationToken cancellationToken = default);
        Task<RoleListItemDto> UpdateRoleAsync(string roleId, UpdateRoleDto dto, string actorUserId, CancellationToken cancellationToken = default);
        Task DeleteRoleAsync(string roleId, string actorUserId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<string>> GetAllPermissionsAsync();
    }
}
