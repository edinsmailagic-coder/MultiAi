namespace MultiAI.Models.DTOs
{
    public class CreateWorkflowConnectionDto
    {
        public int WorkflowId {  get; set; }
        public int SourceNodeId {  get; set; }
        public int TargetNodeId {  get; set; }
    }
}
