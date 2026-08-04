using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration.UserSecrets;
using MultiAI.Models.Data;
using MultiAI.Models.DTOs;
using MultiAI.Models.Entities;
using MultiAI.Services.Interface;

namespace MultiAI.Services
{
    public class WorkflowConnectionService : IWorkflowConnectionService
    {
        private readonly AppDbContext context;

        public WorkflowConnectionService(AppDbContext context)
        {
            this.context = context;
        }
        public async Task<List<WorkflowConnection>> GetAsync(int workflowId)
        {
            return await context.WorkflowConnections.Where(x => x.WorkflowId == workflowId && x.IsDeleted == 0).ToListAsync();
        }
        public async Task<WorkflowConnection> AddAsync(CreateWorkflowConnectionDto dto)
        {
            var workflow = await context.Workflows.FirstOrDefaultAsync(x => x.Id == dto.WorkflowId);
            if (workflow == null)
            {
                throw new Exception("Workflow does not exist");
            }
            var sourceNodeId = await context.WorkflowNodes.FirstOrDefaultAsync(x => x.Id == dto.SourceNodeId);
            if (sourceNodeId == null)
                throw new Exception("Source node does not exist");

            var targetNodeId= await context.WorkflowNodes.FirstOrDefaultAsync(x=>x.Id == dto.TargetNodeId);
            if (targetNodeId == null)
                throw new Exception("Target node does not exist");

            var exists = await context.WorkflowConnections.AnyAsync(x =>
                x.WorkflowId == dto.WorkflowId &&
                x.SourceNodeId == dto.SourceNodeId &&
                x.TargetNodeId == dto.TargetNodeId &&
                x.IsDeleted == 0);

            if (exists)
                throw new Exception("Connection already exists.");

            var connection = new WorkflowConnection
            {
                WorkflowId = dto.WorkflowId,
                SourceNodeId = dto.SourceNodeId,
                TargetNodeId = dto.TargetNodeId,

                CreatedAt = DateTime.UtcNow,
                CreatedBy = workflow.User.UserName,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = workflow.User.UserName
            };

            context.WorkflowConnections.Add(connection);

            await context.SaveChangesAsync();

            return connection;
        }

        public async Task DeleteAsync(int Id,string userId)
        {
            var connection = await context.WorkflowConnections.FirstOrDefaultAsync(x=>x.Id==Id);

            if (connection == null) throw new Exception("Connection not found");
            var user = await context.Users.FirstOrDefaultAsync(x => x.Id == userId);
            connection.IsDeleted = 1;
            connection.DeletedAt= DateTime.UtcNow;
            connection.DeletedBy = user.UserName;

            await context.SaveChangesAsync();
        }
    }
}
