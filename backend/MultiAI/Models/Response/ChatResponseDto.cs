namespace MultiAI.Models.Response
{
    public class ChatResponseDto
    {
        public string Model {  get; set; }
        public string Content {  get; set; }
        public int PromptTokens {  get; set; }
        public int CompletitionTokens { get; set; }
        public int TotalTokens {  get; set; }
    }
}
