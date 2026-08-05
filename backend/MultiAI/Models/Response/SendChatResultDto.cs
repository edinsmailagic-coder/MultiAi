namespace MultiAI.Models.Response
{
    /// <summary>
    /// Result of sending a chat message, including conversation context.
    /// </summary>
    public class SendChatResultDto
    {
        public int ConversationId { get; set; }
        public string Title { get; set; } = string.Empty;
        public ChatMessageResultDto UserMessage { get; set; } = null!;
        public ChatResponseDto Response { get; set; } = null!;
    }

    public class ChatMessageResultDto
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
