namespace MultiAI.Models.Response
{
    public class ConversationDetailDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<ConversationTurnDto> Turns { get; set; } = [];
    }

    public class ConversationTurnDto
    {
        public ChatMessageResultDto UserMessage { get; set; } = null!;
        public List<ChatResponseDto> Responses { get; set; } = [];
    }
}
