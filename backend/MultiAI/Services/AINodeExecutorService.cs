using Microsoft.EntityFrameworkCore;
using MultiAI.Models.Data;
using MultiAI.Models.DTOs;
using MultiAI.Models.Entities;
using MultiAI.Models.Workflows;
using MultiAI.Services.Interface;
using System.Text;

namespace MultiAI.Services
{
    public class AINodeExecutorService : INodeExecutorService
    {
        private readonly AppDbContext context;
        private readonly IAIProviderFactory providerFactory;
        private readonly IApiKeyEncryptionService encryptionService;
        private readonly IUsageLogService usageLog;
        public string NodeType => "AI";

        public AINodeExecutorService(
            AppDbContext context,
            IAIProviderFactory providerFactory,
            IApiKeyEncryptionService encryptionService,
            IUsageLogService usageLog)
        {
            this.context = context;
            this.providerFactory = providerFactory;
            this.encryptionService = encryptionService;
            this.usageLog = usageLog;
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

                string content;
                int promptTokens;
                int completionTokens;
                int totalTokens;

                if (executionContext.ShouldStreamNode(node.Id) && executionContext.OnTokenDelta != null)
                {
                    var builder = new StringBuilder();
                    promptTokens = 0;
                    completionTokens = 0;
                    totalTokens = 0;

                    await foreach (var chunk in provider.ChatStream(
                                       messages,
                                       node.Model,
                                       apiKey,
                                       executionContext.CancellationToken))
                    {
                        if (!string.IsNullOrEmpty(chunk.Delta))
                        {
                            builder.Append(chunk.Delta);
                            await executionContext.OnTokenDelta(chunk.Delta, executionContext.CancellationToken);
                        }

                        if (chunk.IsDone)
                        {
                            promptTokens = chunk.PromptTokens;
                            completionTokens = chunk.CompletionTokens;
                            totalTokens = chunk.TotalTokens;
                        }
                    }

                    content = builder.ToString();
                    if (string.IsNullOrWhiteSpace(content))
                        throw new Exception("OpenAI returned an empty response.");
                }
                else
                {
                    var response = await provider.Chat(messages, node.Model, apiKey);
                    content = response.Content;
                    promptTokens = response.PromptTokens;
                    completionTokens = response.CompletitionTokens;
                    totalTokens = response.TotalTokens;
                }

                stopwatch.Stop();

                result.Success = true;
                result.Output = content;
                result.DurationMs = stopwatch.ElapsedMilliseconds;
                result.PromptTokens = promptTokens;
                result.CompletionTokens = completionTokens;
                result.TotalTokens = totalTokens;
                result.Cost = TokenCostEstimator.Estimate(node.Model, promptTokens, completionTokens);
                result.Outputs["output"] = content;

                executionContext.Variables[$"node:{node.Id}:output"] = content;
                executionContext.Variables["lastOutput"] = content;

                await usageLog.LogAsync(
                    executionContext.UserId,
                    userProvider.Id,
                    userProvider.Provider.Name,
                    node.Model,
                    promptTokens,
                    completionTokens,
                    totalTokens,
                    result.Cost,
                    executionContext.WorkflowId,
                    node.Id,
                    "workflow");

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
