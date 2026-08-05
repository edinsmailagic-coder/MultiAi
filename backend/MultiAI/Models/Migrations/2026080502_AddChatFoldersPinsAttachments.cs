using FluentMigrator;

namespace MultiAI.Models.Migrations
{
    [Migration(2026080502)]
    public class AddChatFoldersPinsAttachments : Migration
    {
        public override void Up()
        {
            Create.Table("ConversationFolders")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("UserId").AsString(450).NotNullable()
                .WithColumn("Name").AsString(200).NotNullable()
                .WithColumn("SortOrder").AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn("CreatedAt").AsDateTime().NotNullable()
                .WithColumn("CreatedBy").AsString(100).Nullable()
                .WithColumn("UpdatedAt").AsDateTime().Nullable()
                .WithColumn("UpdatedBy").AsString(100).Nullable()
                .WithColumn("DeletedAt").AsDateTime().Nullable()
                .WithColumn("DeletedBy").AsString(100).Nullable()
                .WithColumn("IsDeleted").AsInt32().NotNullable().WithDefaultValue(0);

            Create.Index("IX_ConversationFolders_UserId")
                .OnTable("ConversationFolders")
                .OnColumn("UserId");

            Alter.Table("Conversations")
                .AddColumn("IsPinned").AsBoolean().NotNullable().WithDefaultValue(false)
                .AddColumn("PinnedAt").AsDateTime().Nullable()
                .AddColumn("FolderId").AsInt32().Nullable();

            Create.ForeignKey("FK_Conversations_ConversationFolders_FolderId")
                .FromTable("Conversations").ForeignColumn("FolderId")
                .ToTable("ConversationFolders").PrimaryColumn("Id");

            Create.Index("IX_Conversations_FolderId")
                .OnTable("Conversations")
                .OnColumn("FolderId");

            Create.Table("MessageAttachments")
                .WithColumn("Id").AsInt32().PrimaryKey().Identity()
                .WithColumn("OwnerUserId").AsString(450).NotNullable()
                .WithColumn("UserMessageId").AsInt32().Nullable()
                .WithColumn("FileName").AsString(260).NotNullable()
                .WithColumn("ContentType").AsString(200).NotNullable()
                .WithColumn("SizeBytes").AsInt64().NotNullable()
                .WithColumn("Data").AsBinary(int.MaxValue).NotNullable()
                .WithColumn("ExtractedText").AsString(int.MaxValue).Nullable()
                .WithColumn("IsImage").AsBoolean().NotNullable().WithDefaultValue(false)
                .WithColumn("CreatedAt").AsDateTime().NotNullable()
                .WithColumn("CreatedBy").AsString(100).Nullable()
                .WithColumn("UpdatedAt").AsDateTime().Nullable()
                .WithColumn("UpdatedBy").AsString(100).Nullable()
                .WithColumn("DeletedAt").AsDateTime().Nullable()
                .WithColumn("DeletedBy").AsString(100).Nullable()
                .WithColumn("IsDeleted").AsInt32().NotNullable().WithDefaultValue(0);

            Create.ForeignKey("FK_MessageAttachments_UserMessages_UserMessageId")
                .FromTable("MessageAttachments").ForeignColumn("UserMessageId")
                .ToTable("UserMessages").PrimaryColumn("Id");

            Create.Index("IX_MessageAttachments_OwnerUserId")
                .OnTable("MessageAttachments")
                .OnColumn("OwnerUserId");

            Create.Index("IX_MessageAttachments_UserMessageId")
                .OnTable("MessageAttachments")
                .OnColumn("UserMessageId");
        }

        public override void Down()
        {
            Delete.ForeignKey("FK_MessageAttachments_UserMessages_UserMessageId").OnTable("MessageAttachments");
            Delete.Table("MessageAttachments");

            Delete.ForeignKey("FK_Conversations_ConversationFolders_FolderId").OnTable("Conversations");
            Delete.Index("IX_Conversations_FolderId").OnTable("Conversations");
            Delete.Column("FolderId").FromTable("Conversations");
            Delete.Column("PinnedAt").FromTable("Conversations");
            Delete.Column("IsPinned").FromTable("Conversations");

            Delete.Table("ConversationFolders");
        }
    }
}
