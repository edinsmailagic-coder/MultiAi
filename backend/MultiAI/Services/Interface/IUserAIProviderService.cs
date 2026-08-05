using MultiAI.Models.DTOs;
using MultiAI.Models.Entities;

namespace MultiAI.Services.Interface
{
    public interface IUserAIProviderService
    {
        Task<UserAIProvider> ConnectAsync(string userId, int providerId, string key);
        Task<List<UserAIProvider>> GetConnectedAsync(string userId);
        Task<List<AIModelDto>> GetModelsAsync(string userId, int userProviderId);
        Task<UserAIProvider> SetDefaultModelAsync(string userId, int userProviderId, string defaultModel);
        Task<UserAIProvider> SetDefaultProviderAsync(string userId, int userProviderId);
    }
}
