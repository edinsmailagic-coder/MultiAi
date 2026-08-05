using MultiAI.Models.Workflows;

namespace MultiAI.Services.Interface
{
    public interface IWorkflowExecutionService
    {
        Task<WorkflowExecutionContext> ExecuteAsync(int workflowId, string userId, string input);

        Task<WorkflowExecutionContext> ExecuteAsync(
            int workflowId,
            string userId,
            string input,
            Func<WorkflowNodeProgress, CancellationToken, Task>? onNodeProgress,
            Func<string, CancellationToken, Task>? onTokenDelta,
            CancellationToken cancellationToken = default);
    }
}
