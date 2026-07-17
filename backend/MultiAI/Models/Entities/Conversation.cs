using MultiAI.Models.Entities.Base;

namespace MultiAI.Models.Entities
{
    public class Conversation:BaseEntity
    {
        public string UserId {  get; set; }
        public string Title {  get; set; }
    }
}
