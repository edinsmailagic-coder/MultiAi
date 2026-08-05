using FluentMigrator;

namespace MultiAI.Models.Migrations
{
    [Migration(2026080503)]
    public class AddWorkflowConnectionBranching : Migration
    {
        public override void Up()
        {
            Alter.Table("WorkflowConnections")
                .AddColumn("ConditionKey").AsString(100).Nullable()
                .AddColumn("Priority").AsInt32().NotNullable().WithDefaultValue(0);
        }

        public override void Down()
        {
            Delete.Column("Priority").FromTable("WorkflowConnections");
            Delete.Column("ConditionKey").FromTable("WorkflowConnections");
        }
    }
}
