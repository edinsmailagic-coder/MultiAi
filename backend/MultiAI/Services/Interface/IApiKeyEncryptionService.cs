namespace MultiAI.Services.Interface
{
    public interface IApiKeyEncryptionService
    {
        string Encrypt (string key);
        string Decrypt (string encryptedKey);
    }
}
