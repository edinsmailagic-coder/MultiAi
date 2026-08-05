using MultiAI.Models.DTOs;
using MultiAI.Models.Response;
using MultiAI.Services.Interface;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MultiAI.Services.Providers
{
    public class OpenAIProvider : IAIProvider
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private readonly HttpClient httpClient;

        public string Name => "OpenAI";

        public OpenAIProvider(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        public async Task<bool> ValidateKey(string key)
        {
            using var request = CreateAuthorizedRequest(HttpMethod.Get, "models", key);
            using var response = await httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }

        public async Task<List<AIModelDto>> GetModels(string key)
        {
            using var request = CreateAuthorizedRequest(HttpMethod.Get, "models", key);
            using var response = await httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("Unable to load models");
            }

            var json = await response.Content.ReadAsStringAsync();
            var models = JsonSerializer.Deserialize<OpenAIModelsResponse>(json, JsonOptions);

            if (models?.Data == null)
                return [];

            return models.Data
                .OrderBy(x => x.Id)
                .Select(x => new AIModelDto
                {
                    Id = x.Id,
                    Name = x.Id,
                    SupportsVision = false,
                    SupportsTools = false,
                    SupportsReasoning = false,
                    MaxTokens = 0
                })
                .ToList();
        }

        public async Task<ChatResponseDto> Chat(List<ChatMessageDto> messages, string model, string key)
        {
            if (string.IsNullOrWhiteSpace(model))
                throw new ArgumentException("Model is required.", nameof(model));

            if (messages == null || messages.Count == 0)
                throw new ArgumentException("At least one message is required.", nameof(messages));

            var payloadMessages = messages.Select(m =>
            {
                if (m.Images.Count == 0)
                {
                    return (object)new { role = m.Role, content = m.Content };
                }

                var parts = new List<object>
                {
                    new { type = "text", text = m.Content }
                };

                foreach (var image in m.Images)
                {
                    parts.Add(new
                    {
                        type = "image_url",
                        image_url = new
                        {
                            url = $"data:{image.MimeType};base64,{image.Base64Data}"
                        }
                    });
                }

                return new { role = m.Role, content = parts };
            });

            var payload = new
            {
                model,
                messages = payloadMessages
            };

            using var request = CreateAuthorizedRequest(HttpMethod.Post, "chat/completions", key);
            request.Content = new StringContent(
                JsonSerializer.Serialize(payload, JsonOptions),
                Encoding.UTF8,
                "application/json");

            using var response = await httpClient.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"OpenAI chat failed ({(int)response.StatusCode}): {TrimError(body)}");
            }

            var completion = JsonSerializer.Deserialize<OpenAIChatCompletionResponse>(body, JsonOptions);
            var choice = completion?.Choices?.FirstOrDefault();
            var content = choice?.Message?.Content;

            if (string.IsNullOrWhiteSpace(content))
                throw new Exception("OpenAI returned an empty response.");

            return new ChatResponseDto
            {
                Model = completion?.Model ?? model,
                Content = content,
                PromptTokens = completion?.Usage?.PromptTokens ?? 0,
                CompletitionTokens = completion?.Usage?.CompletionTokens ?? 0,
                TotalTokens = completion?.Usage?.TotalTokens ?? 0
            };
        }

        private static HttpRequestMessage CreateAuthorizedRequest(HttpMethod method, string relativeUrl, string key)
        {
            var request = new HttpRequestMessage(method, relativeUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
            return request;
        }

        private static string TrimError(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
                return "Unknown error";

            return body.Length <= 500 ? body : body[..500];
        }

        private sealed class OpenAIChatCompletionResponse
        {
            public string? Model { get; set; }
            public List<OpenAIChoice>? Choices { get; set; }
            public OpenAIUsage? Usage { get; set; }
        }

        private sealed class OpenAIChoice
        {
            public OpenAIMessage? Message { get; set; }
        }

        private sealed class OpenAIMessage
        {
            public string? Role { get; set; }
            public string? Content { get; set; }
        }

        private sealed class OpenAIUsage
        {
            [JsonPropertyName("prompt_tokens")]
            public int PromptTokens { get; set; }

            [JsonPropertyName("completion_tokens")]
            public int CompletionTokens { get; set; }

            [JsonPropertyName("total_tokens")]
            public int TotalTokens { get; set; }
        }
    }
}
