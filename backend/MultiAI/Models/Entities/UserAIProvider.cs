using MultiAI.Models.Entities.Base;
using System.ComponentModel.DataAnnotations.Schema;

namespace MultiAI.Models.Entities
{
    public class UserAIProvider:BaseEntity
    {
        public string UserId {  get; set; }
        public int ProviderId {  get; set; }
        public AIProvider Provider { get; set; }
        public string EncryptedApiKey {  get; set; }
        public bool IsDefault { get; set; }
        [NotMapped]
        public string? DisplayName { get; set; }
    }
}
