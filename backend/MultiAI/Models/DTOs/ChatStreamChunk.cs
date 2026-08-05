namespace MultiAI.Models.DTOs
{
    public class ChatStreamChunk
    {
        public string? Delta { get; set; }
        public bool IsDone { get; set; }
        public string? Model { get; set; }
        public int PromptTokens { get; set; }
        public int CompletionTokens { get; set; }
        public int TotalTokens { get; set; }
    }
}
