using MultiAI.Models.Entities.Base;

namespace MultiAI.Models.Entities
{
    public class AIProvider:BaseEntity
    {
        public string Name { get; set; }
        public bool SupportsModelListing {  get; set; }
        public bool IsActive { get; set; }
    }
}
