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
            services.AddScoped<IWorkflowService, WorkflowService>();
            services.AddScoped<IWorkflowNodeService, WorkflowNodeService>();
            services.AddScoped<IWorkflowConnectionService, WorkflowConnectionService>();
            services.AddScoped<INodeExecutorService, AINodeExecutorService>();
            services.AddScoped<IWorkflowEngine, WorkflowEngineService>();
            services.AddScoped<IWorkflowExecutionService, WorkflowExecutionService>();

            return services;
        }
    }
}
