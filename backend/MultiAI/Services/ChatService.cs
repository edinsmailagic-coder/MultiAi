using Microsoft.EntityFrameworkCore;
using MultiAI.Models.Data;
using MultiAI.Models.Request;
using MultiAI.Models.Response;
using MultiAI.Services.Interface;

namespace MultiAI.Services
{
    public class ChatService : IChatService
    {
        private readonly AppDbContext context;
        private readonly IAIProviderFactory providerFactory;
        private readonly IApiKeyEncryptionService encryptionService;

        public ChatService(AppDbContext context,IAIProviderFactory providerFactory, IApiKeyEncryptionService encryptionService)
        {
            this.context = context;
            this.providerFactory = providerFactory;
            this.encryptionService = encryptionService;
        }

        public async Task<string> SendAsync(int userId,ChatRequestDto request)
        {
            return "null";
            //var userProvider= await context.UserAIProviders.Include(x=>x.Provider).FirstOrDefault(x=>x.Id == request.UserProviderId)
        }
    }
}
