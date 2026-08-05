using Microsoft.EntityFrameworkCore;
using MultiAI.Models.Data;
using MultiAI.Models.Entities;
using MultiAI.Services.Interface;

namespace MultiAI.Services
{
    public class WorkflowService : IWorkflowService
    {
        private readonly AppDbContext dbContext;

        public WorkflowService(AppDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<List<Workflow>> GetAllAsync(string userId)
        {
            return await dbContext.Workflows.Where(x => x.UserId == userId && x.IsDeleted==0).ToListAsync();
        }
        
        public async Task<Workflow?> GetByIdAsync(int workflowId, string userId)
        {
            return await dbContext.Workflows.Where(x => x.UserId == userId && x.Id == workflowId && x.IsDeleted == 0).FirstOrDefaultAsync();
        }
        
        public async Task<Workflow> CreateAsync(string userId, string name, string? description)
        {
            var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId);
            var newWorkflow = new Workflow
            {
                UserId = userId,
                Name = name,
                Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = user.UserName,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = user.UserName
            };

            dbContext.Workflows.Add(newWorkflow);
            await dbContext.SaveChangesAsync();
            return newWorkflow;
        }
        
        public async Task UpdateAsync(Workflow workflow)
        {

            var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == workflow.UserId);
            workflow.UpdatedAt = DateTime.UtcNow;
            workflow.UpdatedBy=user.UserName;

            dbContext.Workflows.Update(workflow);

            await dbContext.SaveChangesAsync();
        }
        
        public async Task DeleteAsync(int workflowId, string userId)
        {
            var workflow = await GetByIdAsync(workflowId, userId);

            if (workflow == null)
                throw new Exception("Workflow not found.");

            workflow.IsDeleted = 1;
            workflow.DeletedAt = DateTime.UtcNow;

            await dbContext.SaveChangesAsync();
        }
    }
}
