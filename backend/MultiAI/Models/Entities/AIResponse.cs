using System.ComponentModel.DataAnnotations.Schema;
using MultiAI.Models.Entities.Base;

namespace MultiAI.Models.Entities
{
    public class AIResponse : BaseEntity
    {
        public int UserMessageId { get; set; }
        public string Model { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int PromptTokens { get; set; }

        [Column("CompletionTokens")]
        public int CompletitionTokens { get; set; }

        public int TotalTokens { get; set; }
        public decimal? Cost { get; set; }
        public long DurationMs { get; set; }
        public bool IsStreamingCompleted { get; set; }
        public int? WorkflowNodeId { get; set; }

        [Column("UserAIProviderId")]
        public int UserAIProvideId { get; set; }

        [ForeignKey(nameof(UserAIProvideId))]
        public UserAIProvider UserAIProvider { get; set; } = null!;
    }
}
