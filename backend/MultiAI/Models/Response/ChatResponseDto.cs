namespace MultiAI.Models.Response
{
    public class ChatResponseDto
    {
        public int Id { get; set; }
        public string Model { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int PromptTokens { get; set; }
        public int CompletitionTokens { get; set; }
        public int TotalTokens { get; set; }
        public long DurationMs { get; set; }
        public int? UserProviderId { get; set; }
        public int? WorkflowNodeId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
