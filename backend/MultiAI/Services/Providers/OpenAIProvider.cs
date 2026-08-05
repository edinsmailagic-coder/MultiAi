using MultiAI.Models.DTOs;
using MultiAI.Models.Response;
using MultiAI.Services.Interface;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
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

            var payload = BuildChatPayload(messages, model, stream: false);

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

        public async IAsyncEnumerable<ChatStreamChunk> ChatStream(
            List<ChatMessageDto> messages,
            string model,
            string key,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(model))
                throw new ArgumentException("Model is required.", nameof(model));

            if (messages == null || messages.Count == 0)
                throw new ArgumentException("At least one message is required.", nameof(messages));

            var payload = BuildChatPayload(messages, model, stream: true);

            using var request = CreateAuthorizedRequest(HttpMethod.Post, "chat/completions", key);
            request.Content = new StringContent(
                JsonSerializer.Serialize(payload, JsonOptions),
                Encoding.UTF8,
                "application/json");

            using var response = await httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new Exception($"OpenAI chat failed ({(int)response.StatusCode}): {TrimError(errorBody)}");
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            string? responseModel = model;
            var promptTokens = 0;
            var completionTokens = 0;
            var totalTokens = 0;

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var line = await reader.ReadLineAsync(cancellationToken);
                if (line == null)
                    break;

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (!line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                    continue;

                var data = line["data:".Length..].Trim();
                if (data.Equals("[DONE]", StringComparison.OrdinalIgnoreCase))
                    break;

                OpenAIChatStreamChunk? chunk;
                try
                {
                    chunk = JsonSerializer.Deserialize<OpenAIChatStreamChunk>(data, JsonOptions);
                }
                catch
                {
                    continue;
                }

                if (chunk == null)
                    continue;

                if (!string.IsNullOrWhiteSpace(chunk.Model))
                    responseModel = chunk.Model;

                if (chunk.Usage != null)
                {
                    promptTokens = chunk.Usage.PromptTokens;
                    completionTokens = chunk.Usage.CompletionTokens;
                    totalTokens = chunk.Usage.TotalTokens;
                }

                var delta = chunk.Choices?.FirstOrDefault()?.Delta?.Content;
                if (string.IsNullOrEmpty(delta))
                    continue;

                yield return new ChatStreamChunk
                {
                    Delta = delta,
                    Model = responseModel,
                    IsDone = false
                };
            }

            yield return new ChatStreamChunk
            {
                IsDone = true,
                Model = responseModel,
                PromptTokens = promptTokens,
                CompletionTokens = completionTokens,
                TotalTokens = totalTokens
            };
        }

        private static object BuildChatPayload(List<ChatMessageDto> messages, string model, bool stream)
        {
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

            if (stream)
            {
                return new
                {
                    model,
                    messages = payloadMessages,
                    stream = true,
                    stream_options = new { include_usage = true }
                };
            }

            return new
            {
                model,
                messages = payloadMessages
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

        private sealed class OpenAIChatStreamChunk
        {
            public string? Model { get; set; }
            public List<OpenAIStreamChoice>? Choices { get; set; }
            public OpenAIUsage? Usage { get; set; }
        }

        private sealed class OpenAIStreamChoice
        {
            public OpenAIDelta? Delta { get; set; }
        }

        private sealed class OpenAIDelta
        {
            public string? Content { get; set; }
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
