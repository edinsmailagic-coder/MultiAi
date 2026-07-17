using MultiAI.Models.DTOs;
using MultiAI.Models.Request;
using MultiAI.Models.Response;
using MultiAI.Services.Interface;
using System.Net.Http.Headers;
using System.Text.Json;

namespace MultiAI.Services.Providers
{
    public class OpenAIProvider : IAIProvider
    {
        private readonly HttpClient httpClient;

        public string Name => "OpenAI";

        public OpenAIProvider(HttpClient httpClient)
        {
            this.httpClient = httpClient;
            Console.WriteLine(httpClient.BaseAddress);
        }
        public async Task<bool> ValidateKey(string key)
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);

            var response = await httpClient.GetAsync("models");

            return response.IsSuccessStatusCode;
        }
        public async Task<List<AIModelDto>> GetModels(string key)
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", key);

            var response = await httpClient.GetAsync("models");

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Unable to load models");
            }

            var json = await response.Content.ReadAsStringAsync();

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            };

            var models = JsonSerializer.Deserialize<OpenAIModelsResponse>(json, options);

            if (models == null)
                return [];

            return models.Data.OrderBy(x=>x.Id).Select(x=> new AIModelDto
            {
                Id=x.Id,
                Name=x.Id,

                SupportsVision=false,
                SupportsTools=false,
                SupportsReasoning=false,
                MaxTokens=0
            }).ToList();
        }

        public async Task<ChatResponseDto> Chat(List<ChatMessageDto> messages,string model,string key)
        {
            throw new NotImplementedException();
        }
    }
}
