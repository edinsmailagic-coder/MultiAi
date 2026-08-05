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
        private readonly IWorkflowExecutionService workflowExecutionService;

        public ChatService(
            AppDbContext context,
            IAIProviderFactory providerFactory,
            IApiKeyEncryptionService encryptionService,
            IWorkflowExecutionService workflowExecutionService)
        {
            this.context = context;
            this.providerFactory = providerFactory;
            this.encryptionService = encryptionService;
            this.workflowExecutionService = workflowExecutionService;
        }

        public async Task<SendChatResultDto> SendAsync(string userId, ChatRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
                throw new Exception("Message is required.");

            var user = await context.Users.FirstOrDefaultAsync(x => x.Id == userId)
                ?? throw new Exception("User not found.");

            if (request.WorkflowId.HasValue)
                return await SendWithWorkflowAsync(user, request);

            if (!request.UserProviderId.HasValue || string.IsNullOrWhiteSpace(request.Model))
                throw new Exception("UserProviderId and Model are required when no workflow is selected.");

            return await SendDirectAsync(user, request);
        }

        public async Task<List<ConversationListItemDto>> GetConversationsAsync(string userId)
        {
            return await context.Conversations
                .AsNoTracking()
                .Where(x => x.UserId == userId && x.IsDeleted == 0)
                .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
                .Select(x => new ConversationListItemDto
                {
                    Id = x.Id,
                    Title = x.Title,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt
                })
                .ToListAsync();
        }

        public async Task<ConversationDetailDto> GetConversationAsync(string userId, int conversationId)
        {
            var conversation = await GetOwnedConversationAsync(userId, conversationId, track: false);

            var messages = await context.UserMessages
                .AsNoTracking()
                .Where(x => x.ConversationId == conversationId && x.IsDeleted == 0)
                .OrderBy(x => x.CreatedAt)
                .ThenBy(x => x.Id)
                .ToListAsync();

            var messageIds = messages.Select(x => x.Id).ToList();

            var responses = await context.AIResponses
                .AsNoTracking()
                .Where(x => messageIds.Contains(x.UserMessageId) && x.IsDeleted == 0)
                .OrderBy(x => x.CreatedAt)
                .ThenBy(x => x.Id)
                .ToListAsync();

            return new ConversationDetailDto
            {
                Id = conversation.Id,
                Title = conversation.Title,
                CreatedAt = conversation.CreatedAt,
                UpdatedAt = conversation.UpdatedAt,
                Turns = messages.Select(message => new ConversationTurnDto
                {
                    UserMessage = new ChatMessageResultDto
                    {
                        Id = message.Id,
                        Content = message.Content,
                        CreatedAt = message.CreatedAt
                    },
                    Responses = responses
                        .Where(r => r.UserMessageId == message.Id)
                        .Select(MapResponse)
                        .ToList()
                }).ToList()
            };
        }

        public async Task<ConversationListItemDto> RenameConversationAsync(string userId, int conversationId, string title)
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new Exception("Title is required.");

            var conversation = await GetOwnedConversationAsync(userId, conversationId, track: true);
            var user = await context.Users.FirstOrDefaultAsync(x => x.Id == userId)
                ?? throw new Exception("User not found.");

            conversation.Title = title.Trim();
            conversation.UpdatedAt = DateTime.UtcNow;
            conversation.UpdatedBy = user.UserName;
            await context.SaveChangesAsync();

            return new ConversationListItemDto
            {
                Id = conversation.Id,
                Title = conversation.Title,
                CreatedAt = conversation.CreatedAt,
                UpdatedAt = conversation.UpdatedAt
            };
        }

        public async Task DeleteConversationAsync(string userId, int conversationId)
        {
            var conversation = await GetOwnedConversationAsync(userId, conversationId, track: true);
            var user = await context.Users.FirstOrDefaultAsync(x => x.Id == userId)
                ?? throw new Exception("User not found.");

            conversation.IsDeleted = 1;
            conversation.DeletedAt = DateTime.UtcNow;
            conversation.DeletedBy = user.UserName;
            conversation.UpdatedAt = DateTime.UtcNow;
            conversation.UpdatedBy = user.UserName;
            await context.SaveChangesAsync();
        }

        private async Task<SendChatResultDto> SendDirectAsync(Users user, ChatRequestDto request)
        {
            var conversation = await ResolveConversationAsync(user, request.ConversationId, request.Message);

            var historyMessages = await BuildHistoryAsync(conversation.Id);

            var userProvider = await context.UserAIProviders
                .Include(x => x.Provider)
                .FirstOrDefaultAsync(x =>
                    x.Id == request.UserProviderId &&
                    x.UserId == user.Id &&
                    x.IsDeleted == 0)
                ?? throw new Exception("Provider not found.");

            var userMessage = await PersistUserMessageAsync(user, conversation.Id, request.Message);

            var apiKey = encryptionService.Decrypt(userProvider.EncryptedApiKey);
            var provider = providerFactory.GetProvider(userProvider.Provider.Name);

            historyMessages.Add(new ChatMessageDto
            {
                Role = "user",
                Content = request.Message
            });

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var providerResponse = await provider.Chat(historyMessages, request.Model!, apiKey);
            stopwatch.Stop();

            var aiResponse = new AIResponse
            {
                UserMessageId = userMessage.Id,
                UserAIProvideId = userProvider.Id,
                Model = providerResponse.Model ?? request.Model!,
                Content = providerResponse.Content,
                PromptTokens = providerResponse.PromptTokens,
                CompletitionTokens = providerResponse.CompletitionTokens,
                TotalTokens = providerResponse.TotalTokens,
                DurationMs = stopwatch.ElapsedMilliseconds,
                IsStreamingCompleted = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = user.UserName,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = user.UserName,
                IsDeleted = 0
            };

            context.AIResponses.Add(aiResponse);
            conversation.UpdatedAt = DateTime.UtcNow;
            conversation.UpdatedBy = user.UserName;
            await context.SaveChangesAsync();

            return new SendChatResultDto
            {
                ConversationId = conversation.Id,
                Title = conversation.Title,
                UserMessage = new ChatMessageResultDto
                {
                    Id = userMessage.Id,
                    Content = userMessage.Content,
                    CreatedAt = userMessage.CreatedAt
                },
                Response = MapResponse(aiResponse)
            };
        }

        private async Task<SendChatResultDto> SendWithWorkflowAsync(Users user, ChatRequestDto request)
        {
            var conversation = await ResolveConversationAsync(user, request.ConversationId, request.Message);
            var userMessage = await PersistUserMessageAsync(user, conversation.Id, request.Message);

            var execution = await workflowExecutionService.ExecuteAsync(
                request.WorkflowId!.Value,
                user.Id,
                request.Message);

            var lastResult = execution.NodeResults.Values
                .OrderBy(x => x.ExecutedAt)
                .LastOrDefault(x => x.Success && !string.IsNullOrWhiteSpace(x.Output))
                ?? execution.NodeResults.Values.OrderBy(x => x.ExecutedAt).LastOrDefault()
                ?? throw new Exception("Workflow produced no output.");

            if (!lastResult.Success)
                throw new Exception(lastResult.Error ?? "Workflow execution failed.");

            var node = await context.WorkflowNodes
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == lastResult.NodeId && x.IsDeleted == 0);

            var providerId = node?.UserAIProviderId
                ?? throw new Exception("Unable to resolve workflow provider.");

            // Ensure the provider belongs to the current user.
            var ownedProvider = await context.UserAIProviders
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == providerId && x.UserId == user.Id && x.IsDeleted == 0)
                ?? throw new Exception("Workflow provider not found for current user.");

            var aiResponse = new AIResponse
            {
                UserMessageId = userMessage.Id,
                UserAIProvideId = ownedProvider.Id,
                Model = node?.Model ?? "workflow",
                Content = lastResult.Output ?? string.Empty,
                PromptTokens = lastResult.PromptTokens,
                CompletitionTokens = lastResult.CompletionTokens,
                TotalTokens = lastResult.TotalTokens,
                DurationMs = lastResult.DurationMs,
                Cost = lastResult.Cost,
                WorkflowNodeId = lastResult.NodeId,
                IsStreamingCompleted = true,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = user.UserName,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = user.UserName,
                IsDeleted = 0
            };

            context.AIResponses.Add(aiResponse);
            conversation.UpdatedAt = DateTime.UtcNow;
            conversation.UpdatedBy = user.UserName;
            await context.SaveChangesAsync();

            return new SendChatResultDto
            {
                ConversationId = conversation.Id,
                Title = conversation.Title,
                UserMessage = new ChatMessageResultDto
                {
                    Id = userMessage.Id,
                    Content = userMessage.Content,
                    CreatedAt = userMessage.CreatedAt
                },
                Response = MapResponse(aiResponse)
            };
        }

        private async Task<Conversation> ResolveConversationAsync(Users user, int? conversationId, string message)
        {
            if (conversationId.HasValue)
                return await GetOwnedConversationAsync(user.Id, conversationId.Value, track: true);

            var conversation = new Conversation
            {
                Title = message.Length > 50 ? message[..50] : message,
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = user.UserName,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = user.UserName,
                IsDeleted = 0
            };

            context.Conversations.Add(conversation);
            await context.SaveChangesAsync();
            return conversation;
        }

        private async Task<UserMessage> PersistUserMessageAsync(Users user, int conversationId, string message)
        {
            var userMessage = new UserMessage
            {
                ConversationId = conversationId,
                Content = message,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = user.UserName,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = user.UserName,
                IsDeleted = 0
            };

            context.UserMessages.Add(userMessage);
            await context.SaveChangesAsync();
            return userMessage;
        }

        private async Task<List<ChatMessageDto>> BuildHistoryAsync(int conversationId)
        {
            var messages = await context.UserMessages
                .AsNoTracking()
                .Where(x => x.ConversationId == conversationId && x.IsDeleted == 0)
                .OrderBy(x => x.CreatedAt)
                .ThenBy(x => x.Id)
                .ToListAsync();

            if (messages.Count == 0)
                return [];

            var messageIds = messages.Select(x => x.Id).ToList();
            var responses = await context.AIResponses
                .AsNoTracking()
                .Where(x => messageIds.Contains(x.UserMessageId) && x.IsDeleted == 0)
                .OrderBy(x => x.CreatedAt)
                .ThenBy(x => x.Id)
                .ToListAsync();

            var history = new List<ChatMessageDto>();
            foreach (var message in messages)
            {
                history.Add(new ChatMessageDto { Role = "user", Content = message.Content });

                var assistant = responses
                    .Where(r => r.UserMessageId == message.Id)
                    .Select(r => r.Content)
                    .FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(assistant))
                {
                    history.Add(new ChatMessageDto { Role = "assistant", Content = assistant });
                }
            }

            return history;
        }

        private async Task<Conversation> GetOwnedConversationAsync(string userId, int conversationId, bool track)
        {
            var query = track
                ? context.Conversations.AsQueryable()
                : context.Conversations.AsNoTracking();

            return await query.FirstOrDefaultAsync(x =>
                       x.Id == conversationId &&
                       x.UserId == userId &&
                       x.IsDeleted == 0)
                   ?? throw new Exception("Conversation not found.");
        }

        private static ChatResponseDto MapResponse(AIResponse response) => new()
        {
            Id = response.Id,
            Model = response.Model,
            Content = response.Content,
            PromptTokens = response.PromptTokens,
            CompletitionTokens = response.CompletitionTokens,
            TotalTokens = response.TotalTokens,
            DurationMs = response.DurationMs,
            UserProviderId = response.UserAIProvideId,
            WorkflowNodeId = response.WorkflowNodeId,
            CreatedAt = response.CreatedAt
        };
    }
}
