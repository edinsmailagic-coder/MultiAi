namespace MultiAI.Services.Interface
{
    public interface IAIProviderFactory
    {
        IAIProvider GetProvider(string name);
    }
}
