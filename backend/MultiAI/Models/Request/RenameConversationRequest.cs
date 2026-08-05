using System.ComponentModel.DataAnnotations;

namespace MultiAI.Models.Request
{
    public class RenameConversationRequest
    {
        [Required]
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;
    }
}
