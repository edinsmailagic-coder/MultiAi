using Microsoft.EntityFrameworkCore;
using MultiAI.Models.Data;
using MultiAI.Models.DTOs;
using MultiAI.Services.Interface;

namespace MultiAI.Services
{
    public class UsageStatsService : IUsageStatsService
    {
        private readonly LogDbContext logDb;

        public UsageStatsService(LogDbContext logDb)
        {
            this.logDb = logDb;
        }

        public async Task<UsageStatsDto> GetDashboardStatsAsync(string userId, CancellationToken cancellationToken = default)
        {
            var events = await logDb.UsageEvents
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .ToListAsync(cancellationToken);

            var byProvider = events
                .GroupBy(x => x.ProviderName)
                .Select(g => new ProviderUsageDto
                {
                    ProviderName = g.Key,
                    TotalCost = g.Sum(x => x.Cost),
                    TotalTokens = g.Sum(x => (long)x.TotalTokens),
                    RequestCount = g.Count(),
                })
                .OrderByDescending(x => x.TotalTokens)
                .ToList();

            var topModels = events
                .Where(x => x.WorkflowId != null || x.WorkflowNodeId != null ||
                            string.Equals(x.Source, "workflow", StringComparison.OrdinalIgnoreCase))
                .GroupBy(x => x.Model)
                .Select(g => new ModelUsageDto
                {
                    Model = g.Key,
                    TotalTokens = g.Sum(x => (long)x.TotalTokens),
                    RequestCount = g.Count(),
                })
                .OrderByDescending(x => x.RequestCount)
                .ThenByDescending(x => x.TotalTokens)
                .Take(5)
                .ToList();

            return new UsageStatsDto
            {
                CostByProvider = byProvider.OrderByDescending(x => x.TotalCost).ToList(),
                TokensByProvider = byProvider,
                MostUsedWorkflowModels = topModels,
            };
        }
    }
}
