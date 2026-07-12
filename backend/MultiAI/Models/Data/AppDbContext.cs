using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MultiAI.Models.Entities;

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
        }
    }
}
