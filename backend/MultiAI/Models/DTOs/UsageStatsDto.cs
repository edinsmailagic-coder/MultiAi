namespace MultiAI.Models.DTOs
{
    public class UsageStatsDto
    {
        public List<ProviderUsageDto> CostByProvider { get; set; } = [];
        public List<ProviderUsageDto> TokensByProvider { get; set; } = [];
        public List<ModelUsageDto> MostUsedWorkflowModels { get; set; } = [];
    }

    public class ProviderUsageDto
    {
        public string ProviderName { get; set; } = string.Empty;
        public decimal TotalCost { get; set; }
        public long TotalTokens { get; set; }
        public int RequestCount { get; set; }
    }

    public class ModelUsageDto
    {
        public string Model { get; set; } = string.Empty;
        public long TotalTokens { get; set; }
        public int RequestCount { get; set; }
    }
}
