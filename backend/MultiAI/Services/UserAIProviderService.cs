using Microsoft.EntityFrameworkCore;
using MultiAI.Models.Data;
using MultiAI.Models.DTOs;
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

            var hasAny = await dbContext.UserAIProviders
                .AnyAsync(x => x.UserId == userId && x.IsDeleted == 0);

            var userAIProvider = new UserAIProvider
            {
                UserId = userId,
                ProviderId = providerId,
                EncryptedApiKey = encrypt,
                IsDefault = !hasAny,
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

        /// <summary>
        /// Lists models for a connected provider using the stored encrypted API key.
        /// </summary>
        public async Task<List<AIModelDto>> GetModelsAsync(string userId, int userProviderId)
        {
            var userProvider = await GetOwnedAsync(userId, userProviderId);

            if (!userProvider.Provider.SupportsModelListing)
                return [];

            var apiKey = encryptionService.Decrypt(userProvider.EncryptedApiKey);
            var provider = providerFactory.GetProvider(userProvider.Provider.Name);
            return await provider.GetModels(apiKey);
        }

        /// <summary>
        /// Sets the default model preference for a connected provider.
        /// </summary>
        public async Task<UserAIProvider> SetDefaultModelAsync(string userId, int userProviderId, string defaultModel)
        {
            if (string.IsNullOrWhiteSpace(defaultModel))
                throw new Exception("Default model is required.");

            var userProvider = await GetOwnedTrackedAsync(userId, userProviderId);
            var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId)
                ?? throw new Exception("User not found.");

            userProvider.DefaultModel = defaultModel.Trim();
            userProvider.UpdatedAt = DateTime.UtcNow;
            userProvider.UpdatedBy = user.UserName;
            await dbContext.SaveChangesAsync();

            return userProvider;
        }

        /// <summary>
        /// Marks a connected provider as the user's default provider.
        /// </summary>
        public async Task<UserAIProvider> SetDefaultProviderAsync(string userId, int userProviderId)
        {
            var userProvider = await GetOwnedTrackedAsync(userId, userProviderId);
            var user = await dbContext.Users.FirstOrDefaultAsync(x => x.Id == userId)
                ?? throw new Exception("User not found.");

            var others = await dbContext.UserAIProviders
                .Where(x => x.UserId == userId && x.IsDeleted == 0 && x.Id != userProviderId && x.IsDefault)
                .ToListAsync();

            foreach (var other in others)
            {
                other.IsDefault = false;
                other.UpdatedAt = DateTime.UtcNow;
                other.UpdatedBy = user.UserName;
            }

            userProvider.IsDefault = true;
            userProvider.UpdatedAt = DateTime.UtcNow;
            userProvider.UpdatedBy = user.UserName;
            await dbContext.SaveChangesAsync();

            return userProvider;
        }

        private async Task<UserAIProvider> GetOwnedAsync(string userId, int userProviderId)
        {
            return await dbContext.UserAIProviders
                       .AsNoTracking()
                       .Include(x => x.Provider)
                       .FirstOrDefaultAsync(x =>
                           x.Id == userProviderId &&
                           x.UserId == userId &&
                           x.IsDeleted == 0)
                   ?? throw new Exception("Provider not found.");
        }

        private async Task<UserAIProvider> GetOwnedTrackedAsync(string userId, int userProviderId)
        {
            return await dbContext.UserAIProviders
                       .Include(x => x.Provider)
                       .FirstOrDefaultAsync(x =>
                           x.Id == userProviderId &&
                           x.UserId == userId &&
                           x.IsDeleted == 0)
                   ?? throw new Exception("Provider not found.");
        }
    }
}
