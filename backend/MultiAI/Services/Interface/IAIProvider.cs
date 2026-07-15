using MultiAI.Models.DTOs;
using MultiAI.Models.Request;
using MultiAI.Models.Response;

namespace MultiAI.Services.Interface
{
    public interface IAIProvider
    {
        string Name {  get; }
        Task<bool> ValidateKey(string apiKey);
        Task<List<AIModelDto>> GetModels(string apiKey);
        Task<ChatResponseDto> Chat(ChatRequestDto request,string apiKey);
    }
}
