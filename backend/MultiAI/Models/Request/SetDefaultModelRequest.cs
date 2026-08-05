using System.ComponentModel.DataAnnotations;

namespace MultiAI.Models.Request
{
    public class SetDefaultModelRequest
    {
        [Required]
        [MaxLength(200)]
        public string DefaultModel { get; set; } = string.Empty;
    }
}
