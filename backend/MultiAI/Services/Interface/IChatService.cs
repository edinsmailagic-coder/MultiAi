using MultiAI.Models.Request;
using MultiAI.Models.Response;

namespace MultiAI.Services.Interface
{
    public interface IChatService
    {
        Task<List<ChatResponseDto>> SendAsync(string userId, ChatRequestDto request);
    }
}
