using FluentMigrator;

namespace MultiAI.Models.Migrations
{
    [Migration(2026080401)]
    public class AddOutputAndTargetToWorkflowConnection:Migration
    {
        public override void Up()
        {
            Alter.Table("WorkflowConnections")
                .AddColumn("SourceOutput")
                .AsString(100)
                .NotNullable()
                .WithDefaultValue("output");

            Alter.Table("WorkflowConnections")
                .AddColumn("TargetInput")
                .AsString(100)
                .NotNullable()
                .WithDefaultValue("input");
        }

        public override void Down()
        {
            Delete.Column("SourceOutput")
                .FromTable("WorkflowConnections");

            Delete.Column("TargetInput")
                .FromTable("WorkflowConnections");
        }
    }
}
