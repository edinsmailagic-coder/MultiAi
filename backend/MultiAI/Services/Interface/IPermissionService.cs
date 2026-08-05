using MultiAI.Models.Entities;

namespace MultiAI.Services.Interface
{
    public interface IPermissionService
    {
        Task<IReadOnlyList<string>> GetUserRolesAsync(Users user, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<string>> GetUserPermissionsAsync(Users user, CancellationToken cancellationToken = default);
        Task EnsureDefaultRoleAsync(Users user, CancellationToken cancellationToken = default);
    }
}
