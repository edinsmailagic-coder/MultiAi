namespace MultiAI.Models.DTOs
{
    public class CreateWorkflowNodeDto
    {
        public int WorkflowId { get; set; }

        public int UserAIProviderId { get; set; }

        public string Model { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Prompt { get; set; } = string.Empty;

        public string? InputMapping { get; set; }

        public int Order { get; set; }

        public double PositionX { get; set; }

        public double PositionY { get; set; }

        public string NodeType { get; set; } = "AI";
    }
}
