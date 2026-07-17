using MultiAI.Models.Entities.Base;

namespace MultiAI.Models.Entities
{
    public class WorkflowNode:BaseEntity
    {
        public int WorkflowId {  get; set; }
        public Workflow Workflow { get; set; }
        public int UserAIProviderId {  get; set; }
        public UserAIProvider UserAIProvider { get; set; }
        public string Model {  get; set; }
        public string Prompt {  get; set; }
        public double PositionX { get; set; }
        public double PositionY { get; set; }
        public string NodeType { get; set; } = "AI";
        public int Order {  get; set; }
        public string InputMapping { get; set; }
        public string Name {  get; set; }
    }
}
