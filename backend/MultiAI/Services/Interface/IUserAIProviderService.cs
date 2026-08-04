using MultiAI.Models.Entities;

namespace MultiAI.Services.Interface
{
    public interface IUserAIProviderService
    {
        Task<UserAIProvider> ConnectAsync(string userId, int providerId, string key);
        Task<List<UserAIProvider>> GetConnectedAsync(string userId);
    }
}
