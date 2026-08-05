using MultiAI.Models.Request;
using MultiAI.Models.Response;

namespace MultiAI.Services.Interface
{
    public interface IChatService
    {
        Task<SendChatResultDto> SendAsync(string userId, ChatRequestDto request);
        IAsyncEnumerable<ChatStreamEventDto> StreamAsync(
            string userId,
            ChatRequestDto request,
            CancellationToken cancellationToken = default);
        Task<List<ConversationListItemDto>> GetConversationsAsync(string userId);
        Task<ConversationDetailDto> GetConversationAsync(string userId, int conversationId);
        Task<ConversationListItemDto> RenameConversationAsync(string userId, int conversationId, string title);
        Task DeleteConversationAsync(string userId, int conversationId);
        Task<ConversationListItemDto> SetPinnedAsync(string userId, int conversationId, bool isPinned);
        Task<ConversationListItemDto> AssignFolderAsync(string userId, int conversationId, int? folderId);
    }
}
