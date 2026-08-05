using MultiAI.Models.DTOs;

namespace MultiAI.Services.Interface
{
    public interface IUsageStatsService
    {
        Task<UsageStatsDto> GetDashboardStatsAsync(string userId, CancellationToken cancellationToken = default);
    }
}
