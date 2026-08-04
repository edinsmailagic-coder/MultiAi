namespace MultiAI.Models.DTOs
{
    public class WorkflowExecutionResultDto
    {
        public int WorkflowId { get; set; }
        public string InitialInput { get; set; } = string.Empty;
        public bool Success { get; set; }
        public IReadOnlyList<NodeExecutionResultDto> NodeResults { get; set; } = [];
    }

    public class NodeExecutionResultDto
    {
        public int NodeId { get; set; }
        public bool Success { get; set; }
        public string? Output { get; set; }
        public string? Error { get; set; }
        public long DurationMs { get; set; }
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
        public decimal? Cost { get; set; }
        public DateTime ExecutedAt { get; set; }
    }
}
