using Microsoft.EntityFrameworkCore;
using MultiAI.Models.Data;
using MultiAI.Models.DTOs;
using MultiAI.Models.Entities;
using MultiAI.Models.Request;
using MultiAI.Models.Response;
using MultiAI.Models.Workflows;
using MultiAI.Services.Interface;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;

namespace MultiAI.Services
{
    public class ChatService : IChatService
    {
        private readonly AppDbContext context;
        private readonly IAIProviderFactory providerFactory;
        private readonly IApiKeyEncryptionService encryptionService;
        private readonly IWorkflowExecutionService workflowExecutionService;
        private readonly IMessageAttachmentService attachmentService;
        private readonly IUsageLogService usageLog;

        public ChatService(
            AppDbContext context,
            IAIProviderFactory providerFactory,
            IApiKeyEncryptionService encryptionService,
            IWorkflowExecutionService workflowExecutionService,
            IMessageAttachmentService attachmentService,
            IUsageLogService usageLog)
        {
            this.context = context;
            this.providerFactory = providerFactory;
            this.encryptionService = encryptionService;
            this.workflowExecutionService = workflowExecutionService;
            this.attachmentService = attachmentService;
            this.usageLog = usageLog;
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

        public async IAsyncEnumerable<ChatStreamEventDto> StreamAsync(
            string userId,
            ChatRequestDto request,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
                throw new Exception("Message is required.");

            var user = await context.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
                ?? throw new Exception("User not found.");

            if (request.WorkflowId.HasValue)
            {
                await foreach (var evt in StreamWithWorkflowAsync(user, request, cancellationToken))
                    yield return evt;
                yield break;
            }

            if (!request.UserProviderId.HasValue || string.IsNullOrWhiteSpace(request.Model))
                throw new Exception("UserProviderId and Model are required when no workflow is selected.");

            await foreach (var evt in StreamDirectAsync(user, request, cancellationToken))
                yield return evt;
        }

        public async Task<List<ConversationListItemDto>> GetConversationsAsync(string userId)
        {
            var rows = await context.Conversations
                .AsNoTracking()
                .Where(x => x.UserId == userId && x.IsDeleted == 0)
                .OrderByDescending(x => x.IsPinned)
                .ThenByDescending(x => x.PinnedAt)
                .ThenByDescending(x => x.UpdatedAt ?? x.CreatedAt)
                .ToListAsync();

            return rows.Select(MapListItem).ToList();
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

            var attachments = await context.MessageAttachments
                .AsNoTracking()
                .Where(x => x.UserMessageId != null && messageIds.Contains(x.UserMessageId.Value) && x.IsDeleted == 0)
                .ToListAsync();

            return new ConversationDetailDto
            {
                Id = conversation.Id,
                Title = conversation.Title,
                CreatedAt = conversation.CreatedAt,
                UpdatedAt = conversation.UpdatedAt,
                IsPinned = conversation.IsPinned,
                FolderId = conversation.FolderId,
                Turns = messages.Select(message => new ConversationTurnDto
                {
                    UserMessage = new ChatMessageResultDto
                    {
                        Id = message.Id,
                        Content = message.Content,
                        CreatedAt = message.CreatedAt,
                        Attachments = attachments
                            .Where(a => a.UserMessageId == message.Id)
                            .Select(MapAttachmentMeta)
                            .ToList()
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
            return MapListItem(conversation);
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

        public async Task<ConversationListItemDto> SetPinnedAsync(string userId, int conversationId, bool isPinned)
        {
            var conversation = await GetOwnedConversationAsync(userId, conversationId, track: true);
            var user = await context.Users.FirstOrDefaultAsync(x => x.Id == userId)
                ?? throw new Exception("User not found.");

            conversation.IsPinned = isPinned;
            conversation.PinnedAt = isPinned ? DateTime.UtcNow : null;
            conversation.UpdatedAt = DateTime.UtcNow;
            conversation.UpdatedBy = user.UserName;
            await context.SaveChangesAsync();
            return MapListItem(conversation);
        }

        public async Task<ConversationListItemDto> AssignFolderAsync(string userId, int conversationId, int? folderId)
        {
            var conversation = await GetOwnedConversationAsync(userId, conversationId, track: true);
            var user = await context.Users.FirstOrDefaultAsync(x => x.Id == userId)
                ?? throw new Exception("User not found.");

            if (folderId.HasValue)
            {
                var folderExists = await context.ConversationFolders.AnyAsync(x =>
                    x.Id == folderId.Value && x.UserId == userId && x.IsDeleted == 0);
                if (!folderExists)
                    throw new Exception("Folder not found.");
            }

            conversation.FolderId = folderId;
            conversation.UpdatedAt = DateTime.UtcNow;
            conversation.UpdatedBy = user.UserName;
            await context.SaveChangesAsync();
            return MapListItem(conversation);
        }

        private async Task<SendChatResultDto> SendDirectAsync(Users user, ChatRequestDto request)
        {
            var conversation = await ResolveConversationAsync(user, request.ConversationId, request.Message);
            var pending = await attachmentService.GetOwnedPendingAsync(user.Id, request.AttachmentIds);
            if (request.AttachmentIds.Count > 0 && pending.Count != request.AttachmentIds.Distinct().Count())
                throw new Exception("One or more attachments were not found.");

            var historyMessages = await BuildHistoryAsync(conversation.Id);
            var userProvider = await context.UserAIProviders
                .Include(x => x.Provider)
                .FirstOrDefaultAsync(x =>
                    x.Id == request.UserProviderId &&
                    x.UserId == user.Id &&
                    x.IsDeleted == 0)
                ?? throw new Exception("Provider not found.");

            var userMessage = await PersistUserMessageAsync(user, conversation.Id, request.Message);
            await attachmentService.LinkToMessageAsync(user.Id, userMessage.Id, request.AttachmentIds);

            var chatUserMessage = BuildProviderUserMessage(request.Message, pending);
            historyMessages.Add(chatUserMessage);

            if (chatUserMessage.Images.Count > 0 &&
                !userProvider.Provider.Name.Contains("OpenAI", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Selected provider does not support image inputs.");

            var apiKey = encryptionService.Decrypt(userProvider.EncryptedApiKey);
            var provider = providerFactory.GetProvider(userProvider.Provider.Name);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var providerResponse = await provider.Chat(historyMessages, request.Model!, apiKey);
            stopwatch.Stop();

            var cost = TokenCostEstimator.Estimate(
                providerResponse.Model ?? request.Model!,
                providerResponse.PromptTokens,
                providerResponse.CompletitionTokens);

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
                Cost = cost,
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

            await usageLog.LogAsync(
                user.Id,
                userProvider.Id,
                userProvider.Provider.Name,
                aiResponse.Model,
                aiResponse.PromptTokens,
                aiResponse.CompletitionTokens,
                aiResponse.TotalTokens,
                cost,
                workflowId: null,
                workflowNodeId: null,
                source: "chat");

            return new SendChatResultDto
            {
                ConversationId = conversation.Id,
                Title = conversation.Title,
                UserMessage = new ChatMessageResultDto
                {
                    Id = userMessage.Id,
                    Content = userMessage.Content,
                    CreatedAt = userMessage.CreatedAt,
                    Attachments = pending.Select(MapAttachmentMeta).ToList()
                },
                Response = MapResponse(aiResponse)
            };
        }

        private async Task<SendChatResultDto> SendWithWorkflowAsync(Users user, ChatRequestDto request)
        {
            var conversation = await ResolveConversationAsync(user, request.ConversationId, request.Message);
            var pending = await attachmentService.GetOwnedPendingAsync(user.Id, request.AttachmentIds);
            if (request.AttachmentIds.Count > 0 && pending.Count != request.AttachmentIds.Distinct().Count())
                throw new Exception("One or more attachments were not found.");

            var enrichedInput = EnrichWithAttachmentText(request.Message, pending);
            var userMessage = await PersistUserMessageAsync(user, conversation.Id, request.Message);
            await attachmentService.LinkToMessageAsync(user.Id, userMessage.Id, request.AttachmentIds);

            var execution = await workflowExecutionService.ExecuteAsync(
                request.WorkflowId!.Value,
                user.Id,
                enrichedInput);

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
                    CreatedAt = userMessage.CreatedAt,
                    Attachments = pending.Select(MapAttachmentMeta).ToList()
                },
                Response = MapResponse(aiResponse)
            };
        }

        private async IAsyncEnumerable<ChatStreamEventDto> StreamDirectAsync(
            Users user,
            ChatRequestDto request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var conversation = await ResolveConversationAsync(user, request.ConversationId, request.Message);
            var pending = await attachmentService.GetOwnedPendingAsync(user.Id, request.AttachmentIds);
            if (request.AttachmentIds.Count > 0 && pending.Count != request.AttachmentIds.Distinct().Count())
                throw new Exception("One or more attachments were not found.");

            var historyMessages = await BuildHistoryAsync(conversation.Id);
            var userProvider = await context.UserAIProviders
                .Include(x => x.Provider)
                .FirstOrDefaultAsync(x =>
                    x.Id == request.UserProviderId &&
                    x.UserId == user.Id &&
                    x.IsDeleted == 0, cancellationToken)
                ?? throw new Exception("Provider not found.");

            var userMessage = await PersistUserMessageAsync(user, conversation.Id, request.Message);
            await attachmentService.LinkToMessageAsync(user.Id, userMessage.Id, request.AttachmentIds);

            var userMessageDto = new ChatMessageResultDto
            {
                Id = userMessage.Id,
                Content = userMessage.Content,
                CreatedAt = userMessage.CreatedAt,
                Attachments = pending.Select(MapAttachmentMeta).ToList()
            };

            yield return new ChatStreamEventDto
            {
                Type = "ready",
                ConversationId = conversation.Id,
                Title = conversation.Title,
                UserMessage = userMessageDto
            };

            var chatUserMessage = BuildProviderUserMessage(request.Message, pending);
            historyMessages.Add(chatUserMessage);

            if (chatUserMessage.Images.Count > 0 &&
                !userProvider.Provider.Name.Contains("OpenAI", StringComparison.OrdinalIgnoreCase))
                throw new Exception("Selected provider does not support image inputs.");

            var apiKey = encryptionService.Decrypt(userProvider.EncryptedApiKey);
            var provider = providerFactory.GetProvider(userProvider.Provider.Name);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var content = new StringBuilder();
            var promptTokens = 0;
            var completionTokens = 0;
            var totalTokens = 0;
            var responseModel = request.Model!;

            await foreach (var chunk in provider.ChatStream(historyMessages, request.Model!, apiKey, cancellationToken))
            {
                if (!string.IsNullOrEmpty(chunk.Delta))
                {
                    content.Append(chunk.Delta);
                    yield return new ChatStreamEventDto
                    {
                        Type = "delta",
                        Content = chunk.Delta
                    };
                }

                if (chunk.IsDone)
                {
                    promptTokens = chunk.PromptTokens;
                    completionTokens = chunk.CompletionTokens;
                    totalTokens = chunk.TotalTokens;
                    if (!string.IsNullOrWhiteSpace(chunk.Model))
                        responseModel = chunk.Model;
                }
            }

            stopwatch.Stop();

            if (string.IsNullOrWhiteSpace(content.ToString()))
                throw new Exception("Empty response from provider.");

            var cost = TokenCostEstimator.Estimate(responseModel, promptTokens, completionTokens);

            var aiResponse = new AIResponse
            {
                UserMessageId = userMessage.Id,
                UserAIProvideId = userProvider.Id,
                Model = responseModel,
                Content = content.ToString(),
                PromptTokens = promptTokens,
                CompletitionTokens = completionTokens,
                TotalTokens = totalTokens,
                DurationMs = stopwatch.ElapsedMilliseconds,
                Cost = cost,
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
            await context.SaveChangesAsync(cancellationToken);

            await usageLog.LogAsync(
                user.Id,
                userProvider.Id,
                userProvider.Provider.Name,
                aiResponse.Model,
                aiResponse.PromptTokens,
                aiResponse.CompletitionTokens,
                aiResponse.TotalTokens,
                cost,
                workflowId: null,
                workflowNodeId: null,
                source: "chat");

            yield return new ChatStreamEventDto
            {
                Type = "done",
                ConversationId = conversation.Id,
                Title = conversation.Title,
                UserMessage = userMessageDto,
                Response = MapResponse(aiResponse)
            };
        }

        private async IAsyncEnumerable<ChatStreamEventDto> StreamWithWorkflowAsync(
            Users user,
            ChatRequestDto request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var conversation = await ResolveConversationAsync(user, request.ConversationId, request.Message);
            var pending = await attachmentService.GetOwnedPendingAsync(user.Id, request.AttachmentIds);
            if (request.AttachmentIds.Count > 0 && pending.Count != request.AttachmentIds.Distinct().Count())
                throw new Exception("One or more attachments were not found.");

            var enrichedInput = EnrichWithAttachmentText(request.Message, pending);
            var userMessage = await PersistUserMessageAsync(user, conversation.Id, request.Message);
            await attachmentService.LinkToMessageAsync(user.Id, userMessage.Id, request.AttachmentIds);

            var userMessageDto = new ChatMessageResultDto
            {
                Id = userMessage.Id,
                Content = userMessage.Content,
                CreatedAt = userMessage.CreatedAt,
                Attachments = pending.Select(MapAttachmentMeta).ToList()
            };

            yield return new ChatStreamEventDto
            {
                Type = "ready",
                ConversationId = conversation.Id,
                Title = conversation.Title,
                UserMessage = userMessageDto
            };

            var channel = Channel.CreateUnbounded<ChatStreamEventDto>(new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = false
            });

            WorkflowExecutionContext? execution = null;
            var produceFailed = false;
            var produceTask = ProduceWorkflowEventsAsync(
                channel.Writer,
                request.WorkflowId!.Value,
                user.Id,
                enrichedInput,
                cancellationToken);

            async Task ProduceWorkflowEventsAsync(
                ChannelWriter<ChatStreamEventDto> writer,
                int workflowId,
                string uid,
                string input,
                CancellationToken ct)
            {
                try
                {
                    execution = await workflowExecutionService.ExecuteAsync(
                        workflowId,
                        uid,
                        input,
                        async (progress, token) =>
                        {
                            await writer.WriteAsync(new ChatStreamEventDto
                            {
                                Type = "node-progress",
                                NodeId = progress.NodeId,
                                NodeName = progress.NodeName,
                                NodeType = progress.NodeType,
                                Status = progress.Status,
                                Message = progress.Error
                            }, token);
                        },
                        async (delta, token) =>
                        {
                            await writer.WriteAsync(new ChatStreamEventDto
                            {
                                Type = "delta",
                                Content = delta
                            }, token);
                        },
                        ct);
                }
                catch (Exception ex)
                {
                    produceFailed = true;
                    await writer.WriteAsync(new ChatStreamEventDto
                    {
                        Type = "error",
                        Message = ex.Message
                    }, CancellationToken.None);
                }
                finally
                {
                    writer.TryComplete();
                }
            }

            await foreach (var evt in channel.Reader.ReadAllAsync(cancellationToken))
                yield return evt;

            await produceTask;

            if (produceFailed || execution == null)
                yield break;

            var lastResult = execution.NodeResults.Values
                .OrderBy(x => x.ExecutedAt)
                .LastOrDefault(x => x.Success && !string.IsNullOrWhiteSpace(x.Output))
                ?? execution.NodeResults.Values.OrderBy(x => x.ExecutedAt).LastOrDefault()
                ?? throw new Exception("Workflow produced no output.");

            if (!lastResult.Success)
            {
                yield return new ChatStreamEventDto
                {
                    Type = "error",
                    Message = lastResult.Error ?? "Workflow execution failed."
                };
                yield break;
            }

            var node = await context.WorkflowNodes
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == lastResult.NodeId && x.IsDeleted == 0, cancellationToken);

            var providerId = node?.UserAIProviderId
                ?? throw new Exception("Unable to resolve workflow provider.");

            var ownedProvider = await context.UserAIProviders
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == providerId && x.UserId == user.Id && x.IsDeleted == 0, cancellationToken)
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
            await context.SaveChangesAsync(cancellationToken);

            yield return new ChatStreamEventDto
            {
                Type = "done",
                ConversationId = conversation.Id,
                Title = conversation.Title,
                UserMessage = userMessageDto,
                Response = MapResponse(aiResponse)
            };
        }

        private static ChatMessageDto BuildProviderUserMessage(string message, List<MessageAttachment> attachments)
        {
            var dto = new ChatMessageDto
            {
                Role = "user",
                Content = EnrichWithAttachmentText(message, attachments)
            };

            foreach (var image in attachments.Where(a => a.IsImage))
            {
                dto.Images.Add(new ChatImageDto
                {
                    MimeType = image.ContentType,
                    Base64Data = Convert.ToBase64String(image.Data)
                });
            }

            return dto;
        }

        private static string EnrichWithAttachmentText(string message, List<MessageAttachment> attachments)
        {
            var texts = attachments
                .Where(a => !a.IsImage && !string.IsNullOrWhiteSpace(a.ExtractedText))
                .Select(a => $"--- Attachment: {a.FileName} ---\n{a.ExtractedText}")
                .ToList();

            if (texts.Count == 0)
                return message;

            return $"{message}\n\nAttached document content:\n{string.Join("\n\n", texts)}";
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
                IsDeleted = 0,
                IsPinned = false
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

            var attachmentText = await context.MessageAttachments
                .AsNoTracking()
                .Where(x => x.UserMessageId != null && messageIds.Contains(x.UserMessageId.Value) && x.IsDeleted == 0)
                .ToListAsync();

            var history = new List<ChatMessageDto>();
            foreach (var message in messages)
            {
                var files = attachmentText.Where(a => a.UserMessageId == message.Id).ToList();
                history.Add(new ChatMessageDto
                {
                    Role = "user",
                    Content = EnrichWithAttachmentText(message.Content, files)
                });

                var assistant = responses
                    .Where(r => r.UserMessageId == message.Id)
                    .Select(r => r.Content)
                    .FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(assistant))
                    history.Add(new ChatMessageDto { Role = "assistant", Content = assistant });
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

        private static ConversationListItemDto MapListItem(Conversation x) => new()
        {
            Id = x.Id,
            Title = x.Title,
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt,
            IsPinned = x.IsPinned,
            PinnedAt = x.PinnedAt,
            FolderId = x.FolderId
        };

        private static AttachmentDto MapAttachmentMeta(MessageAttachment x) => new()
        {
            Id = x.Id,
            FileName = x.FileName,
            ContentType = x.ContentType,
            SizeBytes = x.SizeBytes,
            IsImage = x.IsImage,
            ExtractedTextPreview = x.ExtractedText == null
                ? null
                : (x.ExtractedText.Length > 200 ? x.ExtractedText[..200] : x.ExtractedText)
        };

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
