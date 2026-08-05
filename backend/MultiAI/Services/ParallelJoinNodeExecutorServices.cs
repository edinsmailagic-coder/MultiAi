using MultiAI.Models.Entities;
using MultiAI.Models.Workflows;
using MultiAI.Services.Interface;

namespace MultiAI.Services
{
    /// <summary>
    /// Marker node: fork is handled by the workflow engine.
    /// </summary>
    public class ParallelNodeExecutorService : INodeExecutorService
    {
        public string NodeType => "Parallel";

        public Task<NodeExecutionResult> ExecuteAsync(WorkflowExecutionContext executionContext, WorkflowNode node)
        {
            var last = executionContext.Variables.TryGetValue("lastOutput", out var value)
                ? value?.ToString() ?? executionContext.InitialInput
                : executionContext.InitialInput;

            var result = new NodeExecutionResult
            {
                NodeId = node.Id,
                Success = true,
                Output = last,
                ExecutedAt = DateTime.UtcNow
            };
            result.Outputs["output"] = last;
            executionContext.Variables[$"node:{node.Id}:output"] = last;
            return Task.FromResult(result);
        }
    }

    /// <summary>
    /// Marker node: join readiness is handled by the workflow engine.
    /// </summary>
    public class JoinNodeExecutorService : INodeExecutorService
    {
        public string NodeType => "Join";

        public Task<NodeExecutionResult> ExecuteAsync(WorkflowExecutionContext executionContext, WorkflowNode node)
        {
            var last = executionContext.Variables.TryGetValue("lastOutput", out var value)
                ? value?.ToString() ?? executionContext.InitialInput
                : executionContext.InitialInput;

            var result = new NodeExecutionResult
            {
                NodeId = node.Id,
                Success = true,
                Output = last,
                ExecutedAt = DateTime.UtcNow
            };
            result.Outputs["output"] = last;
            executionContext.Variables[$"node:{node.Id}:output"] = last;
            executionContext.Variables["lastOutput"] = last;
            return Task.FromResult(result);
        }
    }
}
