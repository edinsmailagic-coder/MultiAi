namespace MultiAI.Models.DTOs
{
    public class AIModelDto
    {
        public string Id {  get; set; }
        public string Name { get; set; }
        public bool SupportsVision {  get; set; }
        public bool SupportsTools {  get; set; }
        public bool SupportsReasoning {  get; set; }
        public int? MaxTokens { get; set; }
    }
}
