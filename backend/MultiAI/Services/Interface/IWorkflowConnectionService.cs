using MultiAI.Models.DTOs;
using MultiAI.Models.Entities;

namespace MultiAI.Services.Interface
{
    public interface IWorkflowConnectionService
    {
        Task<List<WorkflowConnection>> GetAsync(int workflowId);
        Task<WorkflowConnection> AddAsync(CreateWorkflowConnectionDto dto);
        Task DeleteAsync(int Id,string userId);
    }
}
