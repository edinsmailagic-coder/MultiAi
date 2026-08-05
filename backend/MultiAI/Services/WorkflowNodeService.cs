using IBM.Data.Db2;
using Microsoft.EntityFrameworkCore;
using MultiAI.Models.Data;
using MultiAI.Models.DTOs;
using MultiAI.Models.Entities;
using MultiAI.Services.Interface;
using System.Security.Claims;

namespace MultiAI.Services
{
    public class WorkflowNodeService : IWorkflowNodeService
    {
        private readonly AppDbContext context;

        public WorkflowNodeService(AppDbContext context)
        {
            this.context = context;
        }
        public async Task<List<WorkflowNode>> GetNodesAsync(int workflowId)
        {
            return await context.WorkflowNodes.Where(x=>x.WorkflowId==workflowId && x.IsDeleted==0).ToListAsync();
        }
        public async Task<WorkflowNode> AddNodeAsync (CreateWorkflowNodeDto dto)
        {
            var workflow = await context.Workflows.FirstOrDefaultAsync(x => x.Id == dto.WorkflowId);

            if (workflow == null)
            {
                throw new Exception("Workflow not found");
            }

            var user = await context.Users.FirstOrDefaultAsync(x => x.Id == workflow.UserId);
            var actor = user?.UserName ?? workflow.UserId;

            var nodeType = string.IsNullOrWhiteSpace(dto.NodeType) ? "AI" : dto.NodeType.Trim();
            var providerId = dto.UserAIProviderId;
            if (providerId <= 0 || !string.Equals(nodeType, "AI", StringComparison.OrdinalIgnoreCase))
            {
                var fallback = await context.UserAIProviders
                    .Where(x => x.UserId == workflow.UserId && x.IsDeleted == 0)
                    .OrderByDescending(x => x.IsDefault)
                    .ThenBy(x => x.Id)
                    .Select(x => x.Id)
                    .FirstOrDefaultAsync();
                if (fallback == 0)
                    throw new Exception("Connect an AI provider before adding workflow nodes.");
                if (providerId <= 0)
                    providerId = fallback;
            }

            var node = new WorkflowNode
            {
                WorkflowId = dto.WorkflowId,
                UserAIProviderId = providerId,
                Name = string.IsNullOrWhiteSpace(dto.Name) ? nodeType : dto.Name,
                Model = string.IsNullOrWhiteSpace(dto.Model) ? "-" : dto.Model,
                Prompt = dto.Prompt ?? string.Empty,
                InputMapping = dto.InputMapping,
                Order = dto.Order,
                PositionX = dto.PositionX,
                PositionY = dto.PositionY,
                NodeType = nodeType,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = actor,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = actor
            };

            context.WorkflowNodes.Add(node);
            await context.SaveChangesAsync();
            return node;
        }
        public async Task UpdateNodeAsync(UpdateWorkflowNodeDto dto)
        {
            var node = await context.WorkflowNodes
                .FirstOrDefaultAsync(x => x.Id == dto.Id);

            if (node == null)
                throw new Exception("Workflow node not found.");

            node.Name = dto.Name;
            node.Model = dto.Model;
            node.Prompt = dto.Prompt;
            node.InputMapping = dto.InputMapping;
            node.Order = dto.Order;
            node.NodeType = dto.NodeType;
            node.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();
        }

        public async Task DeleteNodeAsync(int nodeId)
        {
            var node = await context.WorkflowNodes
                .FirstOrDefaultAsync(x => x.Id == nodeId);

            if (node == null)
                throw new Exception("Workflow node not found.");

            node.IsDeleted = 1;
            node.DeletedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();
        }
        public async Task MoveNodeAsync(MoveWorkflowNodeDto dto)
        {
            var node = await context.WorkflowNodes
                .FirstOrDefaultAsync(x => x.Id == dto.NodeId);

            if (node == null)
                throw new Exception("Workflow node not found.");

            node.PositionX = dto.PositionX;
            node.PositionY = dto.PositionY;
            node.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();
        }
    }
}
