namespace MultiAI.Models.Log
{
    public class UsageEvent
    {
        public long Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int UserAIProviderId { get; set; }
        public string ProviderName { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
        public decimal Cost { get; set; }
        public int? WorkflowId { get; set; }
        public int? WorkflowNodeId { get; set; }
        public string Source { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
