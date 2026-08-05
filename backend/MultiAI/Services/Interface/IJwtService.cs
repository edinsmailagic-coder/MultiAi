using MultiAI.Models.Entities;

namespace MultiAI.Services.Interface
{
    public interface IJwtService
    {
        Task<string> CreateTokenAsync(Users user, CancellationToken cancellationToken = default);
    }
}
