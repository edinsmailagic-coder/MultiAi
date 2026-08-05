namespace MultiAI.Services.Interface
{
    public interface IUsageLogService
    {
        Task LogAsync(
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
            CancellationToken cancellationToken = default);
    }
}
