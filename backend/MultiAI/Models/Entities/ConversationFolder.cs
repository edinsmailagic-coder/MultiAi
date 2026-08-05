using MultiAI.Models.Entities.Base;

namespace MultiAI.Models.Entities
{
    public class ConversationFolder : BaseEntity
    {
        public string UserId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int SortOrder { get; set; }
    }
}
