using FluentMigrator;

namespace MultiAI.Models.Migrations
{
    [Migration(2026071701)]
    public class AddTablesForAI:Migration
    {
        public override void Up()
        {
            Create.Table("AIProviders")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()

                .WithColumn("Name").AsString(100).NotNullable()
                .WithColumn("SupportsModelListing").AsBoolean().NotNullable().WithDefaultValue(true)
                .WithColumn("IsActive").AsBoolean().NotNullable().WithDefaultValue(true)

                .WithColumn("CreatedAt").AsDateTime().NotNullable()
                .WithColumn("CreatedBy").AsString(100).Nullable()

                .WithColumn("UpdatedAt").AsDateTime().Nullable()
                .WithColumn("UpdatedBy").AsString(100).Nullable()

                .WithColumn("DeletedAt").AsDateTime().Nullable()
                .WithColumn("DeletedBy").AsString(100).Nullable()

                .WithColumn("IsDeleted").AsInt32().NotNullable().WithDefaultValue(0);

            Create.Table("UserAIProviders")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()

                .WithColumn("UserId").AsString(450).NotNullable()
                .WithColumn("ProviderId").AsInt32().NotNullable()

                .WithColumn("EncryptedApiKey").AsString(int.MaxValue).NotNullable()

                .WithColumn("IsDefault").AsBoolean().NotNullable().WithDefaultValue(false)

                .WithColumn("CreatedAt").AsDateTime().NotNullable()
                .WithColumn("CreatedBy").AsString(100).Nullable()

                .WithColumn("UpdatedAt").AsDateTime().Nullable()
                .WithColumn("UpdatedBy").AsString(100).Nullable()

                .WithColumn("DeletedAt").AsDateTime().Nullable()
                .WithColumn("DeletedBy").AsString(100).Nullable()

                .WithColumn("IsDeleted").AsInt32().NotNullable().WithDefaultValue(0);

            Create.Table("Conversations")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()

                .WithColumn("UserId").AsString(450).NotNullable()

                .WithColumn("Title").AsString(300).NotNullable()

                .WithColumn("CreatedAt").AsDateTime().NotNullable()
                .WithColumn("CreatedBy").AsString(100).Nullable()

                .WithColumn("UpdatedAt").AsDateTime().Nullable()
                .WithColumn("UpdatedBy").AsString(100).Nullable()

                .WithColumn("DeletedAt").AsDateTime().Nullable()
                .WithColumn("DeletedBy").AsString(100).Nullable()

                .WithColumn("IsDeleted").AsInt32().NotNullable().WithDefaultValue(0);

            Create.Table("UserMessages")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()

                .WithColumn("ConversationId").AsInt32().NotNullable()

                .WithColumn("Content").AsString(int.MaxValue).NotNullable()

                .WithColumn("CreatedAt").AsDateTime().NotNullable()
                .WithColumn("CreatedBy").AsString(100).Nullable()

                .WithColumn("UpdatedAt").AsDateTime().Nullable()
                .WithColumn("UpdatedBy").AsString(100).Nullable()

                .WithColumn("DeletedAt").AsDateTime().Nullable()
                .WithColumn("DeletedBy").AsString(100).Nullable()

                .WithColumn("IsDeleted").AsInt32().NotNullable().WithDefaultValue(0);

            Create.Table("AIResponses")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()

                .WithColumn("UserMessageId").AsInt32().NotNullable()

                .WithColumn("UserAIProviderId").AsInt32().NotNullable()

                .WithColumn("Model").AsString(100).NotNullable()

                .WithColumn("Content").AsString(int.MaxValue).NotNullable()

                .WithColumn("PromptTokens").AsInt32().NotNullable().WithDefaultValue(0)

                .WithColumn("CompletionTokens").AsInt32().NotNullable().WithDefaultValue(0)

                .WithColumn("TotalTokens").AsInt32().NotNullable().WithDefaultValue(0)

                .WithColumn("Cost").AsDecimal(18, 6).Nullable()

                .WithColumn("DurationMs").AsInt64().NotNullable().WithDefaultValue(0)

                .WithColumn("IsStreamingCompleted").AsBoolean().NotNullable().WithDefaultValue(false)

                .WithColumn("CreatedAt").AsDateTime().NotNullable()
                .WithColumn("CreatedBy").AsString(100).Nullable()

                .WithColumn("UpdatedAt").AsDateTime().Nullable()
                .WithColumn("UpdatedBy").AsString(100).Nullable()

                .WithColumn("DeletedAt").AsDateTime().Nullable()
                .WithColumn("DeletedBy").AsString(100).Nullable()

                .WithColumn("IsDeleted").AsInt32().NotNullable().WithDefaultValue(0);

            Create.ForeignKey("FK_UserAIProviders_AIProviders")
                .FromTable("UserAIProviders").ForeignColumn("ProviderId")
                .ToTable("AIProviders").PrimaryColumn("Id");

            Create.ForeignKey("FK_UserMessages_Conversations")
                .FromTable("UserMessages").ForeignColumn("ConversationId")
                .ToTable("Conversations").PrimaryColumn("Id");

            Create.ForeignKey("FK_AIResponses_UserMessages")
                .FromTable("AIResponses").ForeignColumn("UserMessageId")
                .ToTable("UserMessages").PrimaryColumn("Id");

            Create.ForeignKey("FK_AIResponses_UserAIProviders")
                .FromTable("AIResponses").ForeignColumn("UserAIProviderId")
                .ToTable("UserAIProviders").PrimaryColumn("Id");

            Create.Index("IX_UserAIProviders_UserId_ProviderId")
                .OnTable("UserAIProviders")
                .OnColumn("UserId").Ascending()
                .OnColumn("ProviderId").Ascending();

            Create.Index("IX_Conversations_UserId")
                .OnTable("Conversations")
                .OnColumn("UserId").Ascending();

            Create.Index("IX_UserMessages_ConversationId")
                .OnTable("UserMessages")
                .OnColumn("ConversationId")
                .Ascending();

            Create.Index("IX_AIResponses_UserMessageId")
                .OnTable("AIResponses")
                .OnColumn("UserMessageId")
                .Ascending();

            Create.Index("IX_AIResponses_UserAIProviderId")
                .OnTable("AIResponses")
                .OnColumn("UserAIProviderId")
                .Ascending();

            Insert.IntoTable("AIProviders")
                .Row(new
                {
                    Name = "OpenAI",
                    SupportsModelListing = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = 0
                });

            Insert.IntoTable("AIProviders")
                .Row(new
                {
                    Name = "Gemini",
                    SupportsModelListing = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = 0
                });

            Insert.IntoTable("AIProviders")
                .Row(new
                {
                    Name = "Anthropic",
                    SupportsModelListing = false,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = 0
                });

            Insert.IntoTable("AIProviders")
                .Row(new
                {
                    Name = "OpenRouter",
                    SupportsModelListing = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = 0
                });

            Insert.IntoTable("AIProviders")
                .Row(new
                {
                    Name = "Ollama",
                    SupportsModelListing = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = 0
                });
        }

        public override void Down()
        {
            Delete.Index("IX_Messages_ConversationId").OnTable("Messages");
            Delete.Index("IX_Conversations_UserId").OnTable("Conversations");
            Delete.Index("IX_UserAIProviders_UserId_ProviderId").OnTable("UserAIProviders");

            Delete.ForeignKey("FK_Messages_Conversations").OnTable("Messages");
            Delete.ForeignKey("FK_UserAIProviders_AIProviders").OnTable("UserAIProviders");
            Delete.ForeignKey("FK_AIResponses_UserAIProviders").OnTable("AIResponses");
            Delete.ForeignKey("FK_AIResponses_UserMessages").OnTable("AIResponses");
            Delete.ForeignKey("FK_UserMessages_Conversations").OnTable("UserMessages");

            Delete.Table("AIResponses");
            Delete.Table("UserMessages");
            Delete.Table("Conversations");
            Delete.Table("UserAIProviders");
            Delete.Table("AIProviders");
        }
    }
}
