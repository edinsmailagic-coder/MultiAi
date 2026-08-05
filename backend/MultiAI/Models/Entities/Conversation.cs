using MultiAI.Models.Entities.Base;

namespace MultiAI.Models.Entities
{
    public class Conversation : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public bool IsPinned { get; set; }
        public DateTime? PinnedAt { get; set; }
        public int? FolderId { get; set; }
    }
}
