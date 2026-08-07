namespace MultiAI.Models.Response
{
    public class ChatStreamEventDto
    {
        public string Type { get; set; } = string.Empty;
        public int? ConversationId { get; set; }
        public string? Title { get; set; }
        public ChatMessageResultDto? UserMessage { get; set; }
        public int? NodeId { get; set; }
        public string? NodeName { get; set; }
        public string? NodeType { get; set; }
        public string? Model { get; set; }
        public string? Status { get; set; }
        public string? Content { get; set; }
        public ChatResponseDto? Response { get; set; }
        public string? Message { get; set; }
    }
}
