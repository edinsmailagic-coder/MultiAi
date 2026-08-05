namespace MultiAI.Models.Response
{
    public class ConversationListItemDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsPinned { get; set; }
        public DateTime? PinnedAt { get; set; }
        public int? FolderId { get; set; }
    }
}
