using Microsoft.EntityFrameworkCore;
using MultiAI.Models.Data;
using MultiAI.Models.Workflows;
using MultiAI.Services.Interface;

namespace MultiAI.Services
{
    public class WorkflowExecutionService : IWorkflowExecutionService
    {
        private readonly AppDbContext context;
        private readonly IWorkflowEngine workflowEngine;

        public WorkflowExecutionService(AppDbContext context,IWorkflowEngine workflowEngine)
        {
            this.context = context;
            this.workflowEngine = workflowEngine;
        }
        public async Task<WorkflowExecutionContext> ExecuteAsync(int workflowId,string userId, string input)
        {
            var workflow = await context.Workflows.FirstOrDefaultAsync(x => x.Id == workflowId && x.UserId == userId && x.IsDeleted == 0);

            if (workflow == null)
                throw new Exception("Workflow not found.");

            var nodes = await context.WorkflowNodes.Where(x =>x.WorkflowId == workflowId && x.IsDeleted == 0).OrderBy(x => x.Order).ToListAsync();

            var connections = await context.WorkflowConnections.Where(x =>x.WorkflowId == workflowId &&x.IsDeleted == 0).ToListAsync();

            
            var executionContext = new WorkflowExecutionContext
            {
                WorkflowId = workflowId,
                UserId = userId,
                InitialInput = input
            };

            await workflowEngine.ExecutionAsync(executionContext,nodes,connections);

            return executionContext;
        }
    }
}
