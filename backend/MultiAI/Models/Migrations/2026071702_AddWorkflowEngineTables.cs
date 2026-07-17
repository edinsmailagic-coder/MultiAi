using FluentMigrator;

namespace MultiAI.Models.Migrations
{
    [Migration(2026071702)]
    public class AddWorkflowEngineTables : Migration
    {
        public override void Up()
        {
            // Dodavanje novih kolona u AIProviders
            Alter.Table("AIProviders")
                .AddColumn("SupportsStreaming")
                .AsBoolean()
                .NotNullable()
                .WithDefaultValue(false)

                .AddColumn("SupportsTools")
                .AsBoolean()
                .NotNullable()
                .WithDefaultValue(false)

                .AddColumn("SupportsVision")
                .AsBoolean()
                .NotNullable()
                .WithDefaultValue(false)

                .AddColumn("SupportsFunctions")
                .AsBoolean()
                .NotNullable()
                .WithDefaultValue(false);


            // Dodavanje Workflow tabele
            Create.Table("Workflows")
                .WithColumn("Id")
                    .AsInt32()
                    .PrimaryKey()
                    .Identity()

                .WithColumn("UserId")
                    .AsString(450)
                    .NotNullable()

                .WithColumn("Name")
                    .AsString(200)
                    .NotNullable()

                .WithColumn("Description")
                    .AsString(int.MaxValue)
                    .Nullable()

                .WithColumn("IsActive")
                    .AsBoolean()
                    .NotNullable()
                    .WithDefaultValue(true)

                .WithColumn("CreatedAt")
                    .AsDateTime()
                    .NotNullable()

                .WithColumn("CreatedBy")
                    .AsString(100)
                    .Nullable()

                .WithColumn("UpdatedAt")
                    .AsDateTime()
                    .Nullable()

                .WithColumn("UpdatedBy")
                    .AsString(100)
                    .Nullable()

                .WithColumn("DeletedAt")
                    .AsDateTime()
                    .Nullable()

                .WithColumn("DeletedBy")
                    .AsString(100)
                    .Nullable()

                .WithColumn("IsDeleted")
                    .AsInt32()
                    .NotNullable()
                    .WithDefaultValue(0);



            // Workflow Nodes
            Create.Table("WorkflowNodes")
                .WithColumn("Id")
                    .AsInt32()
                    .PrimaryKey()
                    .Identity()

                .WithColumn("WorkflowId")
                    .AsInt32()
                    .NotNullable()

                .WithColumn("UserAIProviderId")
                    .AsInt32()
                    .NotNullable()

                .WithColumn("Model")
                    .AsString(200)
                    .NotNullable()

                .WithColumn("Prompt")
                    .AsString(int.MaxValue)
                    .Nullable()

                .WithColumn("PositionX")
                    .AsDouble()
                    .NotNullable()

                .WithColumn("PositionY")
                    .AsDouble()
                    .NotNullable()

                .WithColumn("NodeType")
                    .AsString(50)
                    .NotNullable()
                    .WithDefaultValue("AI")

                .WithColumn("CreatedAt")
                    .AsDateTime()
                    .NotNullable()

                .WithColumn("CreatedBy")
                    .AsString(100)
                    .Nullable()

                .WithColumn("UpdatedAt")
                    .AsDateTime()
                    .Nullable()

                .WithColumn("UpdatedBy")
                    .AsString(100)
                    .Nullable()

                .WithColumn("DeletedAt")
                    .AsDateTime()
                    .Nullable()

                .WithColumn("DeletedBy")
                    .AsString(100)
                    .Nullable()

                .WithColumn("IsDeleted")
                    .AsInt32()
                    .NotNullable()
                    .WithDefaultValue(0)

                .WithColumn("InputMapping")
                    .AsString(int.MaxValue)
                    .Nullable()

                .WithColumn("Order")
                    .AsInt32()
                    .NotNullable()
                    .WithDefaultValue(0)
                .WithColumn("Name")
                    .AsString(int.MaxValue)
                    .Nullable();



            // Workflow Connections
            Create.Table("WorkflowConnections")
                .WithColumn("Id")
                    .AsInt32()
                    .PrimaryKey()
                    .Identity()

                .WithColumn("WorkflowId")
                    .AsInt32()
                    .NotNullable()

                .WithColumn("SourceNodeId")
                    .AsInt32()
                    .NotNullable()

                .WithColumn("TargetNodeId")
                    .AsInt32()
                    .NotNullable()

                .WithColumn("CreatedAt")
                    .AsDateTime()
                    .NotNullable()

                .WithColumn("CreatedBy")
                    .AsString(100)
                    .Nullable()

                .WithColumn("UpdatedAt")
                    .AsDateTime()
                    .Nullable()

                .WithColumn("UpdatedBy")
                    .AsString(100)
                    .Nullable()

                .WithColumn("DeletedAt")
                    .AsDateTime()
                    .Nullable()

                .WithColumn("DeletedBy")
                    .AsString(100)
                    .Nullable()

                .WithColumn("IsDeleted")
                    .AsInt32()
                    .NotNullable()
                    .WithDefaultValue(0);



            // Foreign keys

            Create.ForeignKey("FK_Workflows_Users")
                .FromTable("Workflows")
                .ForeignColumn("UserId")
                .ToTable("Users")
                .PrimaryColumn("Id");


            Create.ForeignKey("FK_WorkflowNodes_Workflows")
                .FromTable("WorkflowNodes")
                .ForeignColumn("WorkflowId")
                .ToTable("Workflows")
                .PrimaryColumn("Id");


            Create.ForeignKey("FK_WorkflowNodes_UserAIProviders")
                .FromTable("WorkflowNodes")
                .ForeignColumn("UserAIProviderId")
                .ToTable("UserAIProviders")
                .PrimaryColumn("Id");


            Create.ForeignKey("FK_WorkflowConnections_Workflows")
                .FromTable("WorkflowConnections")
                .ForeignColumn("WorkflowId")
                .ToTable("Workflows")
                .PrimaryColumn("Id");


            Create.ForeignKey("FK_WorkflowConnections_SourceNode")
                .FromTable("WorkflowConnections")
                .ForeignColumn("SourceNodeId")
                .ToTable("WorkflowNodes")
                .PrimaryColumn("Id");


            Create.ForeignKey("FK_WorkflowConnections_TargetNode")
                .FromTable("WorkflowConnections")
                .ForeignColumn("TargetNodeId")
                .ToTable("WorkflowNodes")
                .PrimaryColumn("Id");


            Create.Index("IX_WorkflowNodes_WorkflowId")
                .OnTable("WorkflowNodes")
                .OnColumn("WorkflowId");


            Create.Index("IX_WorkflowConnections_WorkflowId")
                .OnTable("WorkflowConnections")
                .OnColumn("WorkflowId");
        }


        public override void Down()
        {
            Delete.Index("IX_WorkflowConnections_WorkflowId")
                .OnTable("WorkflowConnections");

            Delete.Index("IX_WorkflowNodes_WorkflowId")
                .OnTable("WorkflowNodes");


            Delete.ForeignKey("FK_WorkflowConnections_TargetNode")
                .OnTable("WorkflowConnections");

            Delete.ForeignKey("FK_WorkflowConnections_SourceNode")
                .OnTable("WorkflowConnections");

            Delete.ForeignKey("FK_WorkflowConnections_Workflows")
                .OnTable("WorkflowConnections");

            Delete.ForeignKey("FK_WorkflowNodes_UserAIProviders")
                .OnTable("WorkflowNodes");

            Delete.ForeignKey("FK_WorkflowNodes_Workflows")
                .OnTable("WorkflowNodes");

            Delete.ForeignKey("FK_Workflows_Users")
                .OnTable("Workflows");


            Delete.Table("WorkflowConnections");

            Delete.Table("WorkflowNodes");

            Delete.Table("Workflows");


            Delete.Column("SupportsStreaming")
                .FromTable("AIProviders");

            Delete.Column("SupportsTools")
                .FromTable("AIProviders");

            Delete.Column("SupportsVision")
                .FromTable("AIProviders");

            Delete.Column("SupportsFunctions")
                .FromTable("AIProviders");
        }
    }
}