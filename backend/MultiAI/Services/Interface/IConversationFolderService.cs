using MultiAI.Models.Response;

namespace MultiAI.Services.Interface
{
    public interface IConversationFolderService
    {
        Task<List<ConversationFolderDto>> GetAsync(string userId);
        Task<ConversationFolderDto> CreateAsync(string userId, string name);
        Task<ConversationFolderDto> RenameAsync(string userId, int folderId, string name);
        Task DeleteAsync(string userId, int folderId);
    }
}
