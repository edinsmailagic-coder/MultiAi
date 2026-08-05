namespace MultiAI.Services
{
    public static class TokenCostEstimator
    {
        // USD per 1M tokens — approximate public list prices for display totals.
        private static readonly Dictionary<string, (decimal Prompt, decimal Completion)> Rates =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["gpt-4o"] = (2.50m, 10.00m),
                ["gpt-4o-mini"] = (0.15m, 0.60m),
                ["gpt-4.1"] = (2.00m, 8.00m),
                ["gpt-4.1-mini"] = (0.40m, 1.60m),
                ["gpt-4.1-nano"] = (0.10m, 0.40m),
                ["gpt-5"] = (1.25m, 10.00m),
                ["gpt-4"] = (30.00m, 60.00m),
                ["gpt-3.5-turbo"] = (0.50m, 1.50m),
            };

        public static decimal Estimate(string? model, int promptTokens, int completionTokens)
        {
            var key = Rates.Keys.FirstOrDefault(k =>
                !string.IsNullOrWhiteSpace(model) &&
                model.Contains(k, StringComparison.OrdinalIgnoreCase));

            var rate = key != null ? Rates[key] : (Prompt: 1.00m, Completion: 3.00m);
            return (promptTokens / 1_000_000m * rate.Prompt) +
                   (completionTokens / 1_000_000m * rate.Completion);
        }
    }
}
