using MultiAI.Models.Entities.Base;

namespace MultiAI.Models.Entities
{
    public class AIResponse:BaseEntity
    {
        public int UserMessageId {  get; set; }
        public string Model {  get; set; }
        public string Content {  get; set; }
        public int PromptTokens {  get; set; }
        public int CompletitionTokens {  get; set; }
        public int TotalTokens {  get; set; }
        public decimal? Cost { get; set; }
        public long DurationMs {  get; set; }
        public bool IsStreamingCompleted {  get; set; }
        public int? WorkflowNodeId {  get; set; }
        public int UserAIProvideId {  get; set; }
        public UserAIProvider UserAIProvider { get; set; }
    }
}
