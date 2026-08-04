using MultiAI.Models.Entities;
using MultiAI.Models.Workflow;

namespace MultiAI.Services.Interface
{
    public interface INodeExecutorService
    {
        string NodeType {  get; }
        Task<NodeExecutionResult> ExecuteAsync(WorkflowExecutionContext context, WorkflowNode node);
    }
}
