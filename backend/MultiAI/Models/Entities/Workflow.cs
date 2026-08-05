using MultiAI.Models.Entities.Base;

namespace MultiAI.Models.Entities
{
    public class Workflow:BaseEntity
    {
        public string UserId {  get; set; }
        public Users User { get; set; }
        public string Name {  get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
    }
}
