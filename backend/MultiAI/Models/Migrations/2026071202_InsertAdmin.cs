using FluentMigrator;
using Microsoft.AspNetCore.Identity;
using MultiAI.Models.Entities;
//pass Admin123!
namespace MultiAI.Models.Migrations
{
    [Migration(2026071202)]
    public class InsertAdmin:Migration
    {
        public override void Up()
        {
            // 1. Generisanje ID-jeva unaprijed da ih možemo povezati
            string adminRoleId = Guid.NewGuid().ToString();
            string adminUserId = Guid.NewGuid().ToString();

            // 2. Kreiranje lozinke pomoću .NET Identity Hasher-a
            var hasher = new PasswordHasher<Users>();
            // Kreiramo lažni objekat korisnika samo za potrebe hashinga
            var dummyUser = new Users { UserName = "admin@multiai.com" };
            string hashedPassword = hasher.HashPassword(dummyUser, "Admin123!");

            // 3. UNOS ROLE: Admin
            Insert.IntoTable("Roles").Row(new
            {
                Id = adminRoleId,
                Name = "Admin",
                NormalizedName = "ADMIN",
                ConcurrencyStamp = Guid.NewGuid().ToString(),
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System",
                IsDeleted = false
            });

            // 4. UNOS KORISNIKA: admin@multiai.com
            Insert.IntoTable("Users").Row(new
            {
                Id = adminUserId,
                UserName = "admin@multiai.com",
                NormalizedUserName = "ADMIN@MULTIAI.COM",
                Email = "admin@multiai.com",
                NormalizedEmail = "ADMIN@MULTIAI.COM",
                EmailConfirmed = true,
                PasswordHash = hashedPassword,
                SecurityStamp = Guid.NewGuid().ToString("D"),
                ConcurrencyStamp = Guid.NewGuid().ToString(),
                PhoneNumberConfirmed = false,
                TwoFactorEnabled = false,
                LockoutEnabled = true,
                AccessFailedCount = 0,
                FirstName = "System",
                LastName = "Administrator",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System",
                IsDeleted = false
            });

            // 5. POVEZIVANJE KORISNIKA I ROLE (UserRoles)
            Insert.IntoTable("UserRoles").Row(new
            {
                UserId = adminUserId,
                RoleId = adminRoleId,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System",
                IsDeleted = false
            });

            // 6. DODAVANJE PERMISIJE (UserClaims)
            // Ovdje korisniku dajemo direktan Claim "Permission" sa vrijednošću "View"
            Insert.IntoTable("UserClaims").Row(new
            {
                UserId = adminUserId,
                ClaimType = "Permission",
                ClaimValue = "View",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "System",
                IsDeleted = false
            });
        }

        public override void Down()
        {
            // U slučaju Rollback-a, brišemo podatke obrnutim redoslijedom
            Delete.FromTable("UserClaims").AllRows();
            Delete.FromTable("UserRoles").AllRows();
            Delete.FromTable("Users").AllRows();
            Delete.FromTable("Roles").AllRows();
        }
    }
}
