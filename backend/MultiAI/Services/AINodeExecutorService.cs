using Microsoft.EntityFrameworkCore;
using MultiAI.Models.Data;
using MultiAI.Models.DTOs;
using MultiAI.Models.Entities;
using MultiAI.Models.Workflows;
using MultiAI.Services.Interface;

namespace MultiAI.Services
{
    public class AINodeExecutorService : INodeExecutorService
    {
        private readonly AppDbContext context;
        private readonly IAIProviderFactory providerFactory;
        private readonly IApiKeyEncryptionService encryptionService;
        public string NodeType => "AI";

        public AINodeExecutorService(AppDbContext context, IAIProviderFactory providerFactory, IApiKeyEncryptionService encryptionService)
        {
            this.context = context;
            this.providerFactory = providerFactory;
            this.encryptionService = encryptionService;
        }

        public async Task<NodeExecutionResult> ExecuteAsync(WorkflowExecutionContext executionContext, WorkflowNode node)
        {
            var result = new NodeExecutionResult
            {
                NodeId = node.Id,
                ExecutedAt = DateTime.UtcNow,
            };

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var userProvider = await context.UserAIProviders
                    .Include(x => x.Provider)
                    .FirstOrDefaultAsync(x => x.Id == node.UserAIProviderId && x.IsDeleted == 0);

                if (userProvider?.Provider == null)
                {
                    throw new Exception("User provider not found");
                }

                var apiKey = encryptionService.Decrypt(userProvider.EncryptedApiKey);
                var provider = providerFactory.GetProvider(userProvider.Provider.Name);
                var messages = BuildMessages(executionContext, node);

                var response = await provider.Chat(messages, node.Model, apiKey);

                stopwatch.Stop();

                result.Success = true;
                result.Output = response.Content;
                result.DurationMs = stopwatch.ElapsedMilliseconds;
                result.PromptTokens = response.PromptTokens;
                result.CompletionTokens = response.CompletitionTokens;
                result.TotalTokens = response.TotalTokens;
                result.Outputs["output"] = response.Content;

                executionContext.Variables[$"node:{node.Id}:output"] = response.Content;
                executionContext.Variables["lastOutput"] = response.Content;

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                result.Success = false;
                result.Error = ex.Message;
                result.DurationMs = stopwatch.ElapsedMilliseconds;
                return result;
            }
        }

        private static List<ChatMessageDto> BuildMessages(WorkflowExecutionContext executionContext, WorkflowNode node)
        {
            var messages = new List<ChatMessageDto>();

            if (!string.IsNullOrWhiteSpace(node.Prompt))
            {
                messages.Add(new ChatMessageDto
                {
                    Role = "system",
                    Content = node.Prompt
                });
            }

            var previousOutput = executionContext.Variables.TryGetValue("lastOutput", out var last)
                ? last?.ToString()
                : null;

            string userContent;
            if (string.IsNullOrWhiteSpace(previousOutput))
            {
                userContent = executionContext.InitialInput;
            }
            else if (!string.Equals(previousOutput, executionContext.InitialInput, StringComparison.Ordinal))
            {
                userContent =
                    $"Original input:\n{executionContext.InitialInput}\n\nPrevious node output:\n{previousOutput}";
            }
            else
            {
                userContent = previousOutput;
            }

            messages.Add(new ChatMessageDto
            {
                Role = "user",
                Content = userContent
            });

            return messages;
        }
    }
}
