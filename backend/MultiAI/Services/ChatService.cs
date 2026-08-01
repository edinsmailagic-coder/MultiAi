using Microsoft.EntityFrameworkCore;
using MultiAI.Models.Data;
using MultiAI.Models.DTOs;
using MultiAI.Models.Entities;
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

        public async Task<List<ChatResponseDto>> SendAsync(string userId,ChatRequestDto request)
        {
            Conversation conversation;
            var user = context.Users.FirstOrDefault(x => x.Id == userId);
            if (request.ConversationId.HasValue)
            {
                conversation = await context.Conversations.FirstOrDefaultAsync(x => x.Id == request.ConversationId) ?? throw new Exception("Conversation not found");
            }
            else
            {
                conversation = new Conversation
                {
                    Title = request.Message.Length > 50 ? request.Message[..50] : request.Message,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = user.UserName,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = user.UserName,
                    IsDeleted = 0
                };
                context.Conversations.Add(conversation);
                await context.SaveChangesAsync();
            }
            var userMessage = new UserMessage
            {
                ConversationId = conversation.Id,
                Content = request.Message,
                CreatedBy = user.UserName,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = user.UserName,
                IsDeleted = 0
            };
            context.UserMessages.Add(userMessage);
            await context.SaveChangesAsync();

            var responses = new List<ChatResponseDto>();

            foreach(var providerRequest in request.Providers)
            {
                var userProvider = await context.UserAIProviders.Include(x => x.Provider).FirstOrDefaultAsync(x => x.Id == providerRequest.UserProviderId);

                if (userProvider == null)
                    throw new Exception("Provider not found");

                var apiKey = encryptionService.Decrypt(userProvider.EncryptedApiKey);

                var provider = providerFactory.GetProvider(userProvider.Provider.Name);

                var messages = new List<ChatMessageDto>
                {
                    new()
                    {
                        Role="user",
                        Content=request.Message
                    }
                };

                var response = await provider.Chat(
                    messages,
                    providerRequest.Model,
                    apiKey
                    );

                responses.Add(response);

                context.AIResponses.Add(new AIResponse
                {
                    UserMessageId = userMessage.Id,
                    UserAIProvideId = userProvider.Id,
                    Model = response.Model,
                    Content = response.Content,
                    PromptTokens = response.PromptTokens,
                    CompletitionTokens = response.CompletitionTokens,
                    TotalTokens = response.TotalTokens,
                    CreatedBy = user.UserName,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = user.UserName,
                    IsDeleted = 0
                });
            }
            await context.SaveChangesAsync();

            return responses;
        }
    }
}
