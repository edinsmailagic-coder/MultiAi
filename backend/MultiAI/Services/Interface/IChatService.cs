using MultiAI.Models.Request;
using MultiAI.Models.Response;

namespace MultiAI.Services.Interface
{
    public interface IChatService
    {
        Task<string> SendAsync(int userId, ChatRequestDto request);
    }
}
