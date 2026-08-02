using Microsoft.AspNetCore.Routing.Constraints;
using MultiAI.Models.DTOs;
using MultiAI.Models.Entities;

namespace MultiAI.Services.Interface
{
    public interface IWorkflowNodeService
    {
        Task<List<WorkflowNode>> GetNodesAsync(int workflowId);
        Task<WorkflowNode> AddNodeAsync(CreateWorkflowNodeDto dto);

        Task UpdateNodeAsync(UpdateWorkflowNodeDto dto);
        Task DeleteNodeAsync(int nodeId);
        Task MoveNodeAsync(MoveWorkflowNodeDto dto);
    }
}
