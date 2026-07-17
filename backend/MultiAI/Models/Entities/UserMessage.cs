using MultiAI.Models.Entities.Base;

namespace MultiAI.Models.Entities
{
    public class UserMessage:BaseEntity
    {
        public int ConversationId {  get; set; }
        public string Content {  get; set; }
    }
}
