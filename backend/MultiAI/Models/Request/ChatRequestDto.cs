using MultiAI.Models.DTOs;

namespace MultiAI.Models.Request
{
    public class ChatRequestDto
    {
        public int UserProviderId {  get; set; }
        public string Model {  get; set; }
        public List<ChatMessageDto> Messages { get; set; } = [];
    }
}
