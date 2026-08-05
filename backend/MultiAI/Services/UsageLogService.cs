using MultiAI.Models.Data;
using MultiAI.Models.Log;
using MultiAI.Services.Interface;

namespace MultiAI.Services
{
    public class UsageLogService : IUsageLogService
    {
        private readonly LogDbContext logDb;

        public UsageLogService(LogDbContext logDb)
        {
            this.logDb = logDb;
        }

        public async Task LogAsync(
            string userId,
            int userAIProviderId,
            string providerName,
            string model,
            int promptTokens,
            int completionTokens,
            int totalTokens,
            decimal? cost,
            int? workflowId,
            int? workflowNodeId,
            string source,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
                return;

            var resolvedCost = cost ?? TokenCostEstimator.Estimate(model, promptTokens, completionTokens);

            logDb.UsageEvents.Add(new UsageEvent
            {
                UserId = userId,
                UserAIProviderId = userAIProviderId,
                ProviderName = string.IsNullOrWhiteSpace(providerName) ? "Unknown" : providerName,
                Model = string.IsNullOrWhiteSpace(model) ? "unknown" : model,
                PromptTokens = promptTokens,
                CompletionTokens = completionTokens,
                TotalTokens = totalTokens > 0 ? totalTokens : promptTokens + completionTokens,
                Cost = resolvedCost,
                WorkflowId = workflowId,
                WorkflowNodeId = workflowNodeId,
                Source = source,
                CreatedAt = DateTime.UtcNow,
            });

            await logDb.SaveChangesAsync(cancellationToken);
        }
    }
}
