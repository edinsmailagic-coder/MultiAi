using Microsoft.EntityFrameworkCore;
using MultiAI.Models.Data;
using MultiAI.Models.Entities;
using MultiAI.Services.Interface;

namespace MultiAI.Services
{
    public class UserAIProviderService : IUserAIProviderService
    {
        private readonly AppDbContext dbContext;
        private readonly IApiKeyEncryptionService encryptionService;
        private readonly IAIProviderFactory providerFactory;

        public UserAIProviderService(
            AppDbContext dbContext,
            IApiKeyEncryptionService encryptionService,
            IAIProviderFactory providerFactory)
        {
            this.dbContext = dbContext;
            this.encryptionService = encryptionService;
            this.providerFactory = providerFactory;
        }

        public async Task<UserAIProvider> ConnectAsync(string userId, int providerId, string key)
        {
            var providerEntity = await dbContext.AIProviders
                                .FirstAsync(x => x.Id == providerId);

            var provider = providerFactory.GetProvider(providerEntity.Name);

            var valid = await provider.ValidateKey(key);

            if (!valid)
            {
                throw new Exception("API key is not valid");

            }
            var existing = await dbContext.UserAIProviders
                            .FirstOrDefaultAsync(x =>
                                x.UserId == userId &&
                                x.ProviderId == providerId &&
                                x.IsDeleted == 0);

            if (existing != null)
            {
                throw new Exception("This provider is already connected.");
            }
            var encrypt = encryptionService.Encrypt(key);

            var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId)
                ?? throw new Exception("User not found.");

            var userAIProvider = new UserAIProvider
            {
                UserId = userId,
                ProviderId = providerId,
                EncryptedApiKey = encrypt,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = user.UserName,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = user.UserName,
                IsDeleted = 0
            };

            dbContext.UserAIProviders.Add(userAIProvider);
            await dbContext.SaveChangesAsync();

            return userAIProvider;
        }

        public Task<List<UserAIProvider>> GetConnectedAsync(string userId)
        {
            return dbContext.UserAIProviders
                .AsNoTracking()
                .Include(x => x.Provider)
                .Where(x => x.UserId == userId && x.IsDeleted == 0)
                .ToListAsync();
        }
    }
}
