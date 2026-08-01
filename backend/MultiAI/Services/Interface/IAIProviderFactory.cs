using MultiAI.Models.Entities;

namespace MultiAI.Services.Interface
{
    public interface IAIProviderFactory
    {
        IAIProvider GetProvider(string name);
        Task<UserAIProvider> AddUserAIProvider(string userId, int providerId, string key);
    }
}
