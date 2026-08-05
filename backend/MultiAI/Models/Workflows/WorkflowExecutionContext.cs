using System.Text.Json.Serialization;

namespace MultiAI.Models.Workflows
{
    public class WorkflowExecutionContext
    {
        public string UserId { get; set; } = string.Empty;

        public int WorkflowId { get; set; }

        public string InitialInput { get; set; } = string.Empty;

        public Dictionary<int, NodeExecutionResult> NodeResults { get; set; }
            = new();

        public List<NodeExecutionResult> ExecutionLog { get; set; } = [];

        public Dictionary<string, object> Variables { get; set; }
            = new();

        [JsonIgnore]
        public CancellationToken CancellationToken { get; set; }

        /// <summary>AI sink nodes whose token deltas should be forwarded to the client.</summary>
        [JsonIgnore]
        public HashSet<int> StreamableNodeIds { get; set; } = [];

        [JsonIgnore]
        public Func<WorkflowNodeProgress, CancellationToken, Task>? OnNodeProgress { get; set; }

        [JsonIgnore]
        public Func<string, CancellationToken, Task>? OnTokenDelta { get; set; }

        public bool ShouldStreamNode(int nodeId) => StreamableNodeIds.Contains(nodeId);
    }
}
