namespace MultiAI.Models.Workflows
{
    public class WorkflowNodeProgress
    {
        public int NodeId { get; set; }
        public string NodeName { get; set; } = string.Empty;
        public string NodeType { get; set; } = string.Empty;
        public string Status { get; set; } = "started"; // started | completed | failed
        public bool Success { get; set; } = true;
        public string? Error { get; set; }
    }
}
