using MultiAI.Models.Entities.Base;

namespace MultiAI.Models.Entities
{
    public class OpenAIModel
    {
        public string Id {  get; set; }
        public string Object {  get; set; }
        public long Created {  get; set; }
        public string OwnedBy {  get; set; }
    }
}
