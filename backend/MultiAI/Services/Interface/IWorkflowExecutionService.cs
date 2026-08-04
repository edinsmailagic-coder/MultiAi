using MultiAI.Models.Workflows;

namespace MultiAI.Services.Interface
{
    public interface IWorkflowExecutionService
    {
        Task<WorkflowExecutionContext> ExecuteAsync(int workflowId, string userId, string input);
    }
}
