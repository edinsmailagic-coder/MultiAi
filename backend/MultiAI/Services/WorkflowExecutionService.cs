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

        public WorkflowExecutionService(AppDbContext context, IWorkflowEngine workflowEngine)
        {
            this.context = context;
            this.workflowEngine = workflowEngine;
        }

        public Task<WorkflowExecutionContext> ExecuteAsync(int workflowId, string userId, string input) =>
            ExecuteAsync(workflowId, userId, input, null, null, CancellationToken.None);

        public async Task<WorkflowExecutionContext> ExecuteAsync(
            int workflowId,
            string userId,
            string input,
            Func<WorkflowNodeProgress, CancellationToken, Task>? onNodeProgress,
            Func<string, CancellationToken, Task>? onTokenDelta,
            CancellationToken cancellationToken = default)
        {
            var workflow = await context.Workflows.FirstOrDefaultAsync(x =>
                x.Id == workflowId && x.UserId == userId && x.IsDeleted == 0);

            if (workflow == null)
                throw new Exception("Workflow not found.");

            var nodes = await context.WorkflowNodes
                .Where(x => x.WorkflowId == workflowId && x.IsDeleted == 0)
                .OrderBy(x => x.Order)
                .ToListAsync(cancellationToken);

            var connections = await context.WorkflowConnections
                .Where(x => x.WorkflowId == workflowId && x.IsDeleted == 0)
                .ToListAsync(cancellationToken);

            var sinkIds = nodes
                .Where(n => !connections.Any(c => c.SourceNodeId == n.Id))
                .Select(n => n.Id)
                .ToHashSet();

            var streamable = nodes
                .Where(n =>
                    sinkIds.Contains(n.Id) &&
                    string.Equals(n.NodeType, "AI", StringComparison.OrdinalIgnoreCase))
                .Select(n => n.Id)
                .ToHashSet();

            // If the graph has no AI sinks (ends on control nodes), stream the last AI node when deltas are requested.
            if (streamable.Count == 0 && onTokenDelta != null)
            {
                var lastAi = nodes
                    .Where(n => string.Equals(n.NodeType, "AI", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(n => n.Order)
                    .ThenByDescending(n => n.Id)
                    .FirstOrDefault();
                if (lastAi != null)
                    streamable.Add(lastAi.Id);
            }

            var executionContext = new WorkflowExecutionContext
            {
                WorkflowId = workflowId,
                UserId = userId,
                InitialInput = input,
                CancellationToken = cancellationToken,
                StreamableNodeIds = onTokenDelta == null ? [] : streamable,
                OnNodeProgress = onNodeProgress,
                OnTokenDelta = onTokenDelta
            };

            await workflowEngine.ExecutionAsync(executionContext, nodes, connections);

            return executionContext;
        }
    }
}
