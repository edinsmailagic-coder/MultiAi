using MultiAI.Models.Request;
using MultiAI.Models.Response;

namespace MultiAI.Services.Interface
{
    public interface IChatService
    {
        Task<SendChatResultDto> SendAsync(string userId, ChatRequestDto request);
        Task<List<ConversationListItemDto>> GetConversationsAsync(string userId);
        Task<ConversationDetailDto> GetConversationAsync(string userId, int conversationId);
        Task<ConversationListItemDto> RenameConversationAsync(string userId, int conversationId, string title);
        Task DeleteConversationAsync(string userId, int conversationId);
    }
}
