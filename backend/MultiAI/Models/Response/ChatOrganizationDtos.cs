namespace MultiAI.Models.Response
{
    public class ConversationFolderDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int SortOrder { get; set; }
    }

    public class CreateFolderRequest
    {
        public string Name { get; set; } = string.Empty;
    }

    public class RenameFolderRequest
    {
        public string Name { get; set; } = string.Empty;
    }

    public class AssignFolderRequest
    {
        public int? FolderId { get; set; }
    }

    public class PinConversationRequest
    {
        public bool IsPinned { get; set; }
    }

    public class AttachmentDto
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public bool IsImage { get; set; }
        public string? ExtractedTextPreview { get; set; }
    }
}
