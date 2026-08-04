namespace MultiAI.Models.Workflow
{
    public class NodeExecutionResult
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

        public Dictionary<string, object> Outputs { get; set; } = new();
        public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    }
}
