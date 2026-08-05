using MultiAI.Models.Entities;

namespace MultiAI.Services.Interface
{
    public interface IWorkflowService
    {
        Task<List<Workflow>> GetAllAsync(string userId);

        Task<Workflow?> GetByIdAsync(int workflowId, string userId);

        Task<Workflow> CreateAsync(string userId, string name, string? description);

        Task UpdateAsync(Workflow workflow);

        Task DeleteAsync(int workflowId,string userId);
    }
}
