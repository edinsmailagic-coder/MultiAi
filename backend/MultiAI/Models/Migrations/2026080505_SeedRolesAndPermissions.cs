using FluentMigrator;

namespace MultiAI.Models.Migrations
{
    [Migration(2026080505)]
    public class SeedRolesAndPermissions : Migration
    {
        private static readonly string[] AllPermissions =
        [
            "auth.me",
            "auth.profile.write",
            "auth.password.change",
            "providers.read",
            "providers.manage",
            "workflows.read",
            "workflows.write",
            "workflows.execute",
            "chat.read",
            "chat.write",
            "usage.read",
        ];

        public override void Up()
        {
            Execute.Sql("""
                IF NOT EXISTS (SELECT 1 FROM [Roles] WHERE [NormalizedName] = N'USER')
                BEGIN
                    INSERT INTO [Roles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp], [CreatedAt], [CreatedBy], [IsDeleted])
                    VALUES (NEWID(), N'User', N'USER', CONVERT(nvarchar(36), NEWID()), SYSUTCDATETIME(), N'System', 0);
                END
                """);

            // Remove obsolete ad-hoc permission claim from early seed.
            Execute.Sql("""
                DELETE FROM [UserClaims]
                WHERE [ClaimType] = N'Permission' AND [ClaimValue] = N'View';
                """);

            SeedRolePermissions("ADMIN", AllPermissions);
            SeedRolePermissions("USER", AllPermissions);

            // Existing users without any role become User (Admin stays Admin).
            Execute.Sql("""
                INSERT INTO [UserRoles] ([UserId], [RoleId], [CreatedAt], [CreatedBy], [IsDeleted])
                SELECT u.[Id], r.[Id], SYSUTCDATETIME(), N'System', 0
                FROM [Users] u
                CROSS JOIN [Roles] r
                WHERE r.[NormalizedName] = N'USER'
                  AND u.[IsDeleted] = 0
                  AND NOT EXISTS (
                      SELECT 1 FROM [UserRoles] ur WHERE ur.[UserId] = u.[Id] AND ur.[IsDeleted] = 0
                  );
                """);
        }

        public override void Down()
        {
            Execute.Sql("""
                DELETE FROM [RoleClaims]
                WHERE [ClaimType] = N'permission';
                """);

            Execute.Sql("""
                DELETE ur
                FROM [UserRoles] ur
                INNER JOIN [Roles] r ON r.[Id] = ur.[RoleId]
                WHERE r.[NormalizedName] = N'USER';
                """);

            Execute.Sql("""
                DELETE FROM [Roles]
                WHERE [NormalizedName] = N'USER';
                """);
        }

        private void SeedRolePermissions(string normalizedRoleName, IEnumerable<string> permissions)
        {
            foreach (var permission in permissions)
            {
                var escaped = permission.Replace("'", "''", StringComparison.Ordinal);
                Execute.Sql($"""
                    INSERT INTO [RoleClaims] ([RoleId], [ClaimType], [ClaimValue], [CreatedAt], [CreatedBy], [IsDeleted])
                    SELECT r.[Id], N'permission', N'{escaped}', SYSUTCDATETIME(), N'System', 0
                    FROM [Roles] r
                    WHERE r.[NormalizedName] = N'{normalizedRoleName}'
                      AND NOT EXISTS (
                          SELECT 1
                          FROM [RoleClaims] rc
                          WHERE rc.[RoleId] = r.[Id]
                            AND rc.[ClaimType] = N'permission'
                            AND rc.[ClaimValue] = N'{escaped}'
                      );
                    """);
            }
        }
    }
}
