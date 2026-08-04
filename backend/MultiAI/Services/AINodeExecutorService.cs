using Microsoft.EntityFrameworkCore;
using MultiAI.Models.Data;
using MultiAI.Models.DTOs;
using MultiAI.Models.Entities;
using MultiAI.Models.Workflow;
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
        public async Task<NodeExecutionResult> ExecuteAsync(WorkflowExecutionContext executionContext,WorkflowNode node)
        {
            var result = new NodeExecutionResult
            {
                NodeId = node.Id,
                ExecutedAt = DateTime.UtcNow,
            };

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var userProvider = await context.UserAIProviders.Include(x => x.Provider).FirstOrDefaultAsync(x => x.Id == node.UserAIProviderId);

                if (userProvider == null)
                {
                    throw new Exception("User provider not found");
                }

                var apiKey = encryptionService.Decrypt(userProvider.EncryptedApiKey);

                var provider = providerFactory.GetProvider(userProvider.Provider.Name);

                var messages = new List<ChatMessageDto>();

                messages.Add(new ChatMessageDto
                {
                    Role = "user",
                    Content = executionContext.InitialInput
                });

                var response = await provider.Chat(messages, node.Model, apiKey);

                stopwatch.Stop();

                result.Success = true;
                result.Output = response.Content;
                result.DurationMs = stopwatch.ElapsedMilliseconds;

                result.PromptTokens = response.PromptTokens;
                result.CompletionTokens = response.CompletitionTokens;
                result.TotalTokens = response.TotalTokens;

                result.Outputs["output"] = response.Content;

                return result;
            }catch (Exception ex)
            {
                stopwatch.Stop();

                result.Success = false;
                result.Error = ex.Message;
                result.DurationMs = stopwatch.ElapsedMilliseconds;

                return result;
            }
        }
    }
}
