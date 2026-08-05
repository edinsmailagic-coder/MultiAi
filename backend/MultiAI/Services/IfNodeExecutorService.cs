using MultiAI.Models.Entities;
using MultiAI.Models.Workflows;
using MultiAI.Services.Interface;

namespace MultiAI.Services
{
    public class IfNodeExecutorService : INodeExecutorService
    {
        public string NodeType => "If";

        public Task<NodeExecutionResult> ExecuteAsync(WorkflowExecutionContext executionContext, WorkflowNode node)
        {
            var result = new NodeExecutionResult
            {
                NodeId = node.Id,
                ExecutedAt = DateTime.UtcNow,
                Success = true
            };

            var source = executionContext.Variables.TryGetValue("lastOutput", out var last)
                ? last?.ToString() ?? string.Empty
                : executionContext.InitialInput;

            var expression = node.Prompt?.Trim() ?? string.Empty;
            var branch = Evaluate(expression, source) ? "true" : "false";

            result.Output = branch;
            result.Outputs["output"] = branch;
            result.Outputs["branch"] = branch;
            executionContext.Variables[$"node:{node.Id}:branch"] = branch;
            executionContext.Variables["lastBranch"] = branch;
            executionContext.Variables["lastOutput"] = source;

            return Task.FromResult(result);
        }

        private static bool Evaluate(string expression, string value)
        {
            if (string.IsNullOrWhiteSpace(expression))
                return !string.IsNullOrWhiteSpace(value);

            var expr = expression.Trim();
            if (expr.StartsWith("contains:", StringComparison.OrdinalIgnoreCase))
            {
                var needle = expr["contains:".Length..].Trim();
                return value.Contains(needle, StringComparison.OrdinalIgnoreCase);
            }

            if (expr.StartsWith("equals:", StringComparison.OrdinalIgnoreCase))
            {
                var expected = expr["equals:".Length..].Trim();
                return string.Equals(value.Trim(), expected, StringComparison.OrdinalIgnoreCase);
            }

            if (expr.Equals("notEmpty", StringComparison.OrdinalIgnoreCase))
                return !string.IsNullOrWhiteSpace(value);

            // Default: treat prompt as substring match.
            return value.Contains(expr, StringComparison.OrdinalIgnoreCase);
        }
    }
}
