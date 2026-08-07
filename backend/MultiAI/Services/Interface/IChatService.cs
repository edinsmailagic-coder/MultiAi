using MultiAI.Models.Request;
using MultiAI.Models.Response;

namespace MultiAI.Services.Interface
{
    public interface IChatService
    {
        Task<SendChatResultDto> SendAsync(string userId, ChatRequestDto request);

        /// <summary>
        /// Streams chat/workflow events. Each call to <paramref name="emit"/> must be awaited
        /// so the HTTP response can flush before the next token/node is produced.
        /// </summary>
        Task StreamAsync(
            string userId,
            ChatRequestDto request,
            Func<ChatStreamEventDto, CancellationToken, Task> emit,
            CancellationToken cancellationToken = default);
        Task<List<ConversationListItemDto>> GetConversationsAsync(string userId);
        Task<ConversationDetailDto> GetConversationAsync(string userId, int conversationId);
        Task<ConversationListItemDto> RenameConversationAsync(string userId, int conversationId, string title);
        Task DeleteConversationAsync(string userId, int conversationId);
        Task<ConversationListItemDto> SetPinnedAsync(string userId, int conversationId, bool isPinned);
        Task<ConversationListItemDto> AssignFolderAsync(string userId, int conversationId, int? folderId);
    }
}
