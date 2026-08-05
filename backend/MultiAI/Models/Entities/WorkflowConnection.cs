using MultiAI.Models.Entities.Base;

namespace MultiAI.Models.Entities
{
    public class WorkflowConnection : BaseEntity
    {
        public int WorkflowId { get; set; }
        public int SourceNodeId { get; set; }
        public int TargetNodeId { get; set; }
        public string SourceOutput { get; set; } = "output";
        public string TargetInput { get; set; } = "input";
        public string? ConditionKey { get; set; }
        public int Priority { get; set; }
    }
}
