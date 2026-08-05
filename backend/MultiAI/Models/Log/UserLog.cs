namespace MultiAI.Models.Log
{
    public class UserLog
    {
        public long Id { get; set; }
        public string? UserId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? Details { get; set; }
        public string? IpAddress { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
