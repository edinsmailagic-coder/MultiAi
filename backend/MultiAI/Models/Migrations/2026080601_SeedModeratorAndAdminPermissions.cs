using FluentMigrator;

namespace MultiAI.Models.Migrations
{
    [Migration(2026080601)]
    public class SeedModeratorAndAdminPermissions : Migration
    {
        private static readonly string[] AdminOnlyPermissions =
        [
            "users.read",
            "users.manage",
            "roles.read",
            "roles.manage",
        ];

        private static readonly string[] ModeratorPermissions =
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
            "users.read",
            "users.manage",
            "roles.read",
            "roles.manage",
        ];

        public override void Up()
        {
            Execute.Sql("""
                IF NOT EXISTS (SELECT 1 FROM [Roles] WHERE [NormalizedName] = N'MODERATOR')
                BEGIN
                    INSERT INTO [Roles] ([Id], [Name], [NormalizedName], [ConcurrencyStamp], [CreatedAt], [CreatedBy], [IsDeleted])
                    VALUES (NEWID(), N'Moderator', N'MODERATOR', CONVERT(nvarchar(36), NEWID()), SYSUTCDATETIME(), N'System', 0);
                END
                """);

            SeedRolePermissions("ADMIN", AdminOnlyPermissions);
            SeedRolePermissions("MODERATOR", ModeratorPermissions);
        }

        public override void Down()
        {
            foreach (var permission in AdminOnlyPermissions)
            {
                var escaped = permission.Replace("'", "''", StringComparison.Ordinal);
                Execute.Sql($"""
                    DELETE rc
                    FROM [RoleClaims] rc
                    INNER JOIN [Roles] r ON r.[Id] = rc.[RoleId]
                    WHERE rc.[ClaimType] = N'permission'
                      AND rc.[ClaimValue] = N'{escaped}'
                      AND r.[NormalizedName] IN (N'ADMIN', N'MODERATOR');
                    """);
            }

            Execute.Sql("""
                DELETE ur
                FROM [UserRoles] ur
                INNER JOIN [Roles] r ON r.[Id] = ur.[RoleId]
                WHERE r.[NormalizedName] = N'MODERATOR';
                """);

            Execute.Sql("""
                DELETE FROM [Roles]
                WHERE [NormalizedName] = N'MODERATOR';
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
