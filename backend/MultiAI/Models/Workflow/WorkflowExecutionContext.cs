namespace MultiAI.Models.Workflow
{
    public class WorkflowExecutionContext
    {
        public string UserId { get; set; } = string.Empty;

        public int WorkflowId { get; set; }

        public string InitialInput { get; set; } = string.Empty;

        public Dictionary<int, NodeExecutionResult> NodeResults { get; set; }
            = new();

        public Dictionary<string, object> Variables { get; set; }
            = new();

        public CancellationToken CancellationToken { get; set; }
    }
}
