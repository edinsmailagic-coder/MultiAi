using MultiAI.Models.Entities;
using MultiAI.Models.Workflows;
using MultiAI.Services.Interface;

namespace MultiAI.Services
{
    public class LoopNodeExecutorService : INodeExecutorService
    {
        public const int DefaultMaxIterations = 5;
        public const int AbsoluteMaxIterations = 20;

        public string NodeType => "Loop";

        public Task<NodeExecutionResult> ExecuteAsync(WorkflowExecutionContext executionContext, WorkflowNode node)
        {
            var key = $"loop:{node.Id}:i";
            var iteration = 0;
            if (executionContext.Variables.TryGetValue(key, out var raw) &&
                int.TryParse(raw?.ToString(), out var parsed))
            {
                iteration = parsed;
            }

            var max = ParseMax(node.Prompt);
            iteration++;
            executionContext.Variables[key] = iteration;

            var continueLoop = iteration <= max;
            var branch = continueLoop ? "body" : "done";

            var result = new NodeExecutionResult
            {
                NodeId = node.Id,
                Success = true,
                Output = branch,
                ExecutedAt = DateTime.UtcNow
            };
            result.Outputs["branch"] = branch;
            result.Outputs["iteration"] = iteration;
            executionContext.Variables[$"node:{node.Id}:branch"] = branch;
            executionContext.Variables["lastBranch"] = branch;

            return Task.FromResult(result);
        }

        private static int ParseMax(string? prompt)
        {
            if (!string.IsNullOrWhiteSpace(prompt) &&
                int.TryParse(prompt.Trim(), out var parsed) &&
                parsed > 0)
            {
                return Math.Min(parsed, AbsoluteMaxIterations);
            }

            return DefaultMaxIterations;
        }
    }
}
