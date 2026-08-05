using MultiAI.Models.Log;

namespace MultiAI.Services.Interface
{
    public interface IUserLogService
    {
        Task LogAsync(
            string? userId,
            string action,
            string? details = null,
            string? ipAddress = null,
            CancellationToken cancellationToken = default);
    }

    public interface IErrorLogService
    {
        Task LogAsync(
            string? userId,
            string path,
            string method,
            Exception exception,
            int statusCode = 500,
            string? source = null,
            CancellationToken cancellationToken = default);
    }
}
