using MultiAI.Services.Interface;

namespace MultiAI.Services
{
    public class AIProviderFactory : IAIProviderFactory
    {
        private readonly IEnumerable<IAIProvider> providers;

        public AIProviderFactory(IEnumerable<IAIProvider> providers)
        {
            this.providers = providers;
        }
        public IAIProvider GetProvider(string name)
        {
            var provider= providers.FirstOrDefault(x=>x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (provider == null)
                throw new Exception($"Provider {name} not found");

            return provider;
        }
    }
}
