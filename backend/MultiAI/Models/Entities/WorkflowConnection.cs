using MultiAI.Models.Entities.Base;

namespace MultiAI.Models.Entities
{
    public class WorkflowConnection:BaseEntity
    {
        public int WorkflowId {  get; set; }
        public int SourceNodeId {  get; set; }
        public int TargetNodeId {  get; set; }
    }
}
