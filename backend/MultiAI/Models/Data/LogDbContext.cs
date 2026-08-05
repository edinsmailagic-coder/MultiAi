using Microsoft.EntityFrameworkCore;
using MultiAI.Models.Log;

namespace MultiAI.Models.Data
{
    public class LogDbContext : DbContext
    {
        public LogDbContext(DbContextOptions<LogDbContext> options) : base(options)
        {
        }

        public DbSet<UsageEvent> UsageEvents { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<UsageEvent>(entity =>
            {
                entity.ToTable("UsageEvents");
                entity.HasKey(x => x.Id);
                entity.Property(x => x.Id).ValueGeneratedOnAdd();
                entity.Property(x => x.UserId).HasMaxLength(450).IsRequired();
                entity.Property(x => x.ProviderName).HasMaxLength(200).IsRequired();
                entity.Property(x => x.Model).HasMaxLength(200).IsRequired();
                entity.Property(x => x.Source).HasMaxLength(50).IsRequired();
                entity.Property(x => x.Cost).HasPrecision(18, 8);
                entity.HasIndex(x => x.UserId);
                entity.HasIndex(x => new { x.UserId, x.ProviderName });
                entity.HasIndex(x => new { x.UserId, x.Model });
                entity.HasIndex(x => x.WorkflowId);
            });
        }
    }
}
