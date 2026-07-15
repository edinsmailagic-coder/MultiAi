using Microsoft.AspNetCore.DataProtection;
using MultiAI.Services.Interface;

namespace MultiAI.Services
{
    public class ApiKeyEncryptionService : IApiKeyEncryptionService
    {
        private readonly IDataProtectionProvider dataProtection;
        private readonly IDataProtector protection;

        public ApiKeyEncryptionService(IDataProtectionProvider dataProtection)
        {
            protection = dataProtection.CreateProtector("AI_API_KEYS");
        }
        public string Encrypt(string key)
        {
            return protection.Protect(key);
        }
        public string Decrypt(string encryptedKey)
        {
            return protection.Unprotect(encryptedKey);
        } 
    }
}
