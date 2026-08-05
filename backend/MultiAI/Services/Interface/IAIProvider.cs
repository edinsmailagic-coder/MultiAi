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
        Task<ChatResponseDto> Chat(List<ChatMessageDto> messages, string model, string key);
        IAsyncEnumerable<ChatStreamChunk> ChatStream(
            List<ChatMessageDto> messages,
            string model,
            string key,
            CancellationToken cancellationToken = default);
    }
}
