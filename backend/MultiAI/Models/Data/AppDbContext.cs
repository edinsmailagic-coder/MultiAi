using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MultiAI.Models.Entities;
using WorkflowEntity = MultiAI.Models.Entities.Workflow;

namespace MultiAI.Models.Data
{
    public class AppDbContext:IdentityDbContext<Users,
    IdentityRole,
    string,
    IdentityUserClaim<string>,
    UserRoles, // <-- Ovdje ubacuješ tvoju custom spojnu klasu
    IdentityUserLogin<string>,
    IdentityRoleClaim<string>,
    IdentityUserToken<string>>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }
        public DbSet<AIProvider> AIProviders { get; set; }

        public DbSet<UserAIProvider> UserAIProviders { get; set; }

        public DbSet<Conversation> Conversations { get; set; }

        public DbSet<UserMessage> UserMessages { get; set; }

        public DbSet<AIResponse> AIResponses { get; set; }
        public DbSet<WorkflowEntity> Workflows { get; set; }

        public DbSet<WorkflowNode> WorkflowNodes { get; set; }

        public DbSet<WorkflowConnection> WorkflowConnections { get; set; }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            // VEOMA VAŽNO: Prvo pozivamo baznu konfiguraciju
            base.OnModelCreating(builder);

            // Mapiramo Identity na tvoje tačne nazive tabela iz FluentMigratora
            builder.Entity<Users>().ToTable("Users");
            builder.Entity<IdentityRole>().ToTable("Roles");
            builder.Entity<UserRoles>().ToTable("UserRoles");
            builder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims");
            builder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims");
            builder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins");
            builder.Entity<IdentityUserToken<string>>().ToTable("UserTokens");
            builder.Entity<AIProvider>().ToTable("AIProviders");
            builder.Entity<AIResponse>().ToTable("AIResponses");
            builder.Entity<Conversation>().ToTable("Conversations");
            builder.Entity<UserAIProvider>().ToTable("UserAIProviders");
            builder.Entity<UserMessage>().ToTable("UserMessages");
            builder.Entity<WorkflowEntity>().ToTable("Workflows");

            builder.Entity<WorkflowNode>().ToTable("WorkflowNodes");

            builder.Entity<WorkflowConnection>().ToTable("WorkflowConnections");

            builder.Entity<AIResponse>(entity =>
            {
                entity.Property(x => x.CompletitionTokens).HasColumnName("CompletionTokens");
                entity.Property(x => x.UserAIProvideId).HasColumnName("UserAIProviderId");
            });
        }
    }
}
