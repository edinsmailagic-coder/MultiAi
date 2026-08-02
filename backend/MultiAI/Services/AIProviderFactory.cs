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

        
    }
}
