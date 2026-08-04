using MultiAI.Models.Workflow;

namespace MultiAI.Services.Interface
{
    public interface IWorkflowExecutionService
    {
        Task<WorkflowExecutionContext> ExecuteAsync(int workflowId, string userId, string input);
    }
}
