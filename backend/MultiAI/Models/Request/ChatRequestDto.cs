using System.ComponentModel.DataAnnotations;

namespace MultiAI.Models.Request
{
    /// <summary>
    /// Request to send a chat message in direct or workflow mode.
    /// </summary>
    public class ChatRequestDto
    {
        public int? ConversationId { get; set; }

        [Required]
        [MinLength(1)]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Required when <see cref="WorkflowId"/> is null.
        /// </summary>
        public int? UserProviderId { get; set; }

        /// <summary>
        /// Required when <see cref="WorkflowId"/> is null.
        /// </summary>
        public string? Model { get; set; }

        /// <summary>
        /// When set, executes the workflow and stores only the final output.
        /// </summary>
        public int? WorkflowId { get; set; }

        /// <summary>
        /// Pending attachment ids uploaded by the user for this message.
        /// </summary>
        public List<int> AttachmentIds { get; set; } = [];
    }
}
