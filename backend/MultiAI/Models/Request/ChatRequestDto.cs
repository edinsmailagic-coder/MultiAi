using MultiAI.Models.DTOs;

namespace MultiAI.Models.Request
{
    public class ChatRequestDto
    {
        public int? ConversationId {  get; set; }
        public string Message {  get; set; }
        public List<ChatProviderRequestDto> Providers { get; set; } = [];
    }
}
