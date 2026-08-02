namespace MultiAI.Models.DTOs
{
    public class UpdateWorkflowNodeDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Model { get; set; } = string.Empty;

        public string Prompt { get; set; } = string.Empty;

        public string? InputMapping { get; set; }

        public int Order { get; set; }

        public string NodeType { get; set; } = "AI";
    }
}
