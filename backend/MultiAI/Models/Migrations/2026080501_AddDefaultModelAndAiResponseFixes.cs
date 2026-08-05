using FluentMigrator;

namespace MultiAI.Models.Migrations
{
    [Migration(2026080501)]
    public class AddDefaultModelAndAiResponseFixes : Migration
    {
        public override void Up()
        {
            Alter.Table("UserAIProviders")
                .AddColumn("DefaultModel")
                .AsString(200)
                .Nullable();

            Alter.Table("AIResponses")
                .AddColumn("WorkflowNodeId")
                .AsInt32()
                .Nullable();
        }

        public override void Down()
        {
            Delete.Column("WorkflowNodeId").FromTable("AIResponses");
            Delete.Column("DefaultModel").FromTable("UserAIProviders");
        }
    }
}
