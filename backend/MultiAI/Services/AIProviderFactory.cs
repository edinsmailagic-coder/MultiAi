using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiAI.Models.Data;
using MultiAI.Models.Entities;
using MultiAI.Services.Interface;

namespace MultiAI.Services
{
    public class AIProviderFactory : IAIProviderFactory
    {
        private readonly IEnumerable<IAIProvider> providers;
        private readonly IAIProvider aIProvider;
        private readonly ApiKeyEncryptionService encryptionService;
        private readonly AppDbContext dbContext;

        public AIProviderFactory(IEnumerable<IAIProvider> providers,IAIProvider aIProvider,ApiKeyEncryptionService encryptionService,AppDbContext dbContext)
        {
            this.providers = providers;
            this.aIProvider = aIProvider;
            this.encryptionService = encryptionService;
            this.dbContext = dbContext;
        }
        public IAIProvider GetProvider(string name)
        {
            var provider= providers.FirstOrDefault(x=>x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (provider == null)
                throw new Exception($"Provider {name} not found");

            return provider;
        }

        public async Task<UserAIProvider> AddUserAIProvider(string userId,int providerId, string key)
        {
            var valid=aIProvider.ValidateKey(key);

            if (valid.IsFaulted)
            {
                throw new Exception("API key is not valid");
                
            }
            var encrypt = encryptionService.Encrypt(key);

            var user = await dbContext.Users.FirstOrDefaultAsync(x=>x.Id == userId);

            var userAIProvider = new UserAIProvider
            {
                UserId = userId,
                ProviderId = providerId,
                EncryptedApiKey = encrypt,
                CreatedAt=DateTime.UtcNow,
                CreatedBy = user.UserName,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = user.UserName,
                IsDeleted = 0
            };

            dbContext.UserAIProviders.Add(userAIProvider);
            await dbContext.SaveChangesAsync();

            return userAIProvider;
        }
    }
}
