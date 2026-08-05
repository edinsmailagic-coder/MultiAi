using MultiAI.Models.Data;
using MultiAI.Models.Log;
using MultiAI.Services.Interface;

namespace MultiAI.Services
{
    public class ErrorLogService : IErrorLogService
    {
        private readonly LogDbContext logDb;

        public ErrorLogService(LogDbContext logDb)
        {
            this.logDb = logDb;
        }

        public async Task LogAsync(
            string? userId,
            string path,
            string method,
            Exception exception,
            int statusCode = 500,
            string? source = null,
            CancellationToken cancellationToken = default)
        {
            logDb.ErrorLogs.Add(new ErrorLog
            {
                UserId = userId,
                Path = path,
                Method = method,
                ExceptionType = exception.GetType().FullName ?? exception.GetType().Name,
                Message = exception.Message,
                StackTrace = exception.StackTrace,
                InnerException = exception.InnerException?.ToString(),
                StatusCode = statusCode,
                Source = source,
                CreatedAt = DateTime.UtcNow,
            });

            await logDb.SaveChangesAsync(cancellationToken);
        }
    }
}
