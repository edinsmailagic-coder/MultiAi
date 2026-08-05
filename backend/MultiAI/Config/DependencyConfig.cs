using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MultiAI.Models.Data;
using MultiAI.Models.Entities;
using MultiAI.Services;
using MultiAI.Services.Interface;
using MultiAI.Services.Providers;

namespace MultiAI.Config
{
    public static class DependencyConfig
    {
        public static IServiceCollection RegisterAppServices (this IServiceCollection services,IConfiguration configuration)
        {
            services.AddDataProtection();
            services.AddDbContext<AppDbContext>(options => options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
            services.AddDbContext<LogDbContext>(options => options.UseSqlServer(configuration.GetConnectionString("LogConnection")));
            services.AddIdentityCore<Users>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddSignInManager()
            .AddApiEndpoints();

            //services.AddAuthentication(IdentityConstants.BearerScheme)
            //    .AddBearerToken(IdentityConstants.BearerScheme);
            services.AddHttpClient<IAIProvider, OpenAIProvider>(client =>
            {
                client.BaseAddress = new Uri("https://api.openai.com/v1/");
                client.Timeout = TimeSpan.FromSeconds(60);
            });
            
            services.AddScoped<IJwtService, JwtService>();

            //services.AddScoped<IAIProvider, OpenAIProvider>();
            services.AddScoped<IAIProviderFactory, AIProviderFactory>();
            services.AddScoped<IApiKeyEncryptionService, ApiKeyEncryptionService>();
            services.AddScoped<IUserAIProviderService, UserAIProviderService>();
            services.AddScoped<IChatService, ChatService>();
            services.AddScoped<IConversationFolderService, ConversationFolderService>();
            services.AddScoped<IMessageAttachmentService, MessageAttachmentService>();
            services.AddScoped<IAttachmentTextExtractor, AttachmentTextExtractor>();
            services.AddScoped<IWorkflowService, WorkflowService>();
            services.AddScoped<IWorkflowNodeService, WorkflowNodeService>();
            services.AddScoped<IWorkflowConnectionService, WorkflowConnectionService>();
            services.AddScoped<INodeExecutorService, AINodeExecutorService>();
            services.AddScoped<INodeExecutorService, IfNodeExecutorService>();
            services.AddScoped<INodeExecutorService, ParallelNodeExecutorService>();
            services.AddScoped<INodeExecutorService, JoinNodeExecutorService>();
            services.AddScoped<INodeExecutorService, LoopNodeExecutorService>();
            services.AddScoped<IWorkflowEngine, WorkflowEngineService>();
            services.AddScoped<IWorkflowExecutionService, WorkflowExecutionService>();
            services.AddScoped<IUsageLogService, UsageLogService>();
            services.AddScoped<IUsageStatsService, UsageStatsService>();

            return services;
        }
    }
}
