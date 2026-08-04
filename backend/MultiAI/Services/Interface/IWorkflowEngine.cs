using MultiAI.Models.Entities;
using MultiAI.Models.Workflows;

namespace MultiAI.Services.Interface
{
    public interface IWorkflowEngine
    {
        Task ExecutionAsync(WorkflowExecutionContext executionContext, List<WorkflowNode> nodes, List<WorkflowConnection> connections);
    }
}
