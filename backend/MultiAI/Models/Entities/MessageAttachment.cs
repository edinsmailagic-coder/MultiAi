using MultiAI.Models.Entities.Base;

namespace MultiAI.Models.Entities
{
    public class MessageAttachment : BaseEntity
    {
        public string OwnerUserId { get; set; } = string.Empty;
        public int? UserMessageId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public string? ExtractedText { get; set; }
        public bool IsImage { get; set; }
    }
}
