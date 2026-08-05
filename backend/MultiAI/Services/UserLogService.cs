using MultiAI.Models.Data;
using MultiAI.Models.Log;
using MultiAI.Services.Interface;

namespace MultiAI.Services
{
    public class UserLogService : IUserLogService
    {
        private readonly LogDbContext logDb;

        public UserLogService(LogDbContext logDb)
        {
            this.logDb = logDb;
        }

        public async Task LogAsync(
            string? userId,
            string action,
            string? details = null,
            string? ipAddress = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(action))
                return;

            logDb.UserLogs.Add(new UserLog
            {
                UserId = userId,
                Action = action,
                Details = details,
                IpAddress = ipAddress,
                CreatedAt = DateTime.UtcNow,
            });

            await logDb.SaveChangesAsync(cancellationToken);
        }
    }
}
