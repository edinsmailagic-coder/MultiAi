namespace MultiAI.Models.DTOs
{
    public class ChatMessageDto
    {
        public string Role { get; set; } = "user";
        public string Content { get; set; } = string.Empty;
        public List<ChatImageDto> Images { get; set; } = [];
    }

    public class ChatImageDto
    {
        public string MimeType { get; set; } = "image/png";
        public string Base64Data { get; set; } = string.Empty;
    }
}
