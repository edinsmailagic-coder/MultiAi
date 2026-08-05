namespace MultiAI.Models.Log
{
    public class ErrorLog
    {
        public long Id { get; set; }
        public string? UserId { get; set; }
        public string Path { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public string ExceptionType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? StackTrace { get; set; }
        public string? InnerException { get; set; }
        public int StatusCode { get; set; }
        public string? Source { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
