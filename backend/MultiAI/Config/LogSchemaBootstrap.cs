using Microsoft.EntityFrameworkCore;
using MultiAI.Models.Data;

namespace MultiAI.Config
{
    public static class LogSchemaBootstrap
    {
        public static void EnsureSchema(LogDbContext logDb)
        {
            logDb.Database.EnsureCreated();

            // EnsureCreated does not add new tables to an existing database.
            logDb.Database.ExecuteSqlRaw("""
                IF OBJECT_ID(N'dbo.UsageEvents', N'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[UsageEvents] (
                        [Id] bigint IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [UserId] nvarchar(450) NOT NULL,
                        [UserAIProviderId] int NOT NULL,
                        [ProviderName] nvarchar(200) NOT NULL,
                        [Model] nvarchar(200) NOT NULL,
                        [PromptTokens] int NOT NULL,
                        [CompletionTokens] int NOT NULL,
                        [TotalTokens] int NOT NULL,
                        [Cost] decimal(18,8) NOT NULL,
                        [WorkflowId] int NULL,
                        [WorkflowNodeId] int NULL,
                        [Source] nvarchar(50) NOT NULL,
                        [CreatedAt] datetime2 NOT NULL
                    );
                    CREATE INDEX [IX_UsageEvents_UserId] ON [dbo].[UsageEvents] ([UserId]);
                    CREATE INDEX [IX_UsageEvents_UserId_ProviderName] ON [dbo].[UsageEvents] ([UserId], [ProviderName]);
                    CREATE INDEX [IX_UsageEvents_UserId_Model] ON [dbo].[UsageEvents] ([UserId], [Model]);
                    CREATE INDEX [IX_UsageEvents_WorkflowId] ON [dbo].[UsageEvents] ([WorkflowId]);
                END
                """);

            logDb.Database.ExecuteSqlRaw("""
                IF OBJECT_ID(N'dbo.ErrorLog', N'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[ErrorLog] (
                        [Id] bigint IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [UserId] nvarchar(450) NULL,
                        [Path] nvarchar(500) NOT NULL,
                        [Method] nvarchar(20) NOT NULL,
                        [ExceptionType] nvarchar(300) NOT NULL,
                        [Message] nvarchar(max) NOT NULL,
                        [StackTrace] nvarchar(max) NULL,
                        [InnerException] nvarchar(max) NULL,
                        [StatusCode] int NOT NULL,
                        [Source] nvarchar(100) NULL,
                        [CreatedAt] datetime2 NOT NULL
                    );
                    CREATE INDEX [IX_ErrorLog_UserId] ON [dbo].[ErrorLog] ([UserId]);
                    CREATE INDEX [IX_ErrorLog_CreatedAt] ON [dbo].[ErrorLog] ([CreatedAt]);
                END
                """);

            logDb.Database.ExecuteSqlRaw("""
                IF OBJECT_ID(N'dbo.UserLog', N'U') IS NULL
                BEGIN
                    CREATE TABLE [dbo].[UserLog] (
                        [Id] bigint IDENTITY(1,1) NOT NULL PRIMARY KEY,
                        [UserId] nvarchar(450) NULL,
                        [Action] nvarchar(100) NOT NULL,
                        [Details] nvarchar(max) NULL,
                        [IpAddress] nvarchar(100) NULL,
                        [CreatedAt] datetime2 NOT NULL
                    );
                    CREATE INDEX [IX_UserLog_UserId] ON [dbo].[UserLog] ([UserId]);
                    CREATE INDEX [IX_UserLog_Action] ON [dbo].[UserLog] ([Action]);
                    CREATE INDEX [IX_UserLog_CreatedAt] ON [dbo].[UserLog] ([CreatedAt]);
                END
                """);
        }
    }
}
