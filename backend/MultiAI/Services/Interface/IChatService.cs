using MultiAI.Models.Request;
using MultiAI.Models.Response;

namespace MultiAI.Services.Interface
{
    public interface IChatService
    {
        Task<ChatResponseDto> SendAsync(int userId, ChatRequestDto request);
    }
}
