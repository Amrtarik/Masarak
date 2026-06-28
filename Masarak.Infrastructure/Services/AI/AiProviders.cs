using Masarak.Application.DTOs;
using Masarak.Application.Interfaces;
using Masarak.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Masarak.Infrastructure.Services.AI
{
    // ═══════════════════════════════════════════════════════════════════
    // OpenAI Provider — Primary provider (gpt-4o)
    // ═══════════════════════════════════════════════════════════════════

    public class OpenAiProvider : IAiProvider
    {
        private readonly HttpClient _httpClient;
        private readonly string _model;
        private readonly ILogger<OpenAiProvider> _logger;

        public string ProviderName => "openai-gpt-4o";

        public OpenAiProvider(IConfiguration config, IHttpClientFactory httpClientFactory,
            ILogger<OpenAiProvider> logger)
        {
            _httpClient = httpClientFactory.CreateClient("OpenAI");
            var apiKey = config["AI:OpenAI:ApiKey"];
            _model = config["AI:OpenAI:Model"] ?? "gpt-4o";
            _logger = logger;

            if (!string.IsNullOrEmpty(apiKey))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", apiKey);
            }
            _httpClient.BaseAddress = new Uri("https://api.openai.com/v1/");
        }

        public async Task<AiCompletionResult> CompleteAsync(AiPromptRequest request, CancellationToken ct)
        {
            var body = new
            {
                model = _model,
                messages = new[]
                {
                    new { role = "system", content = request.SystemPrompt },
                    new { role = "user", content = request.UserPrompt }
                },
                max_tokens = request.MaxTokens,
                temperature = (double)request.Temperature
            };

            var json = JsonSerializer.Serialize(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var response = await _httpClient.PostAsync("chat/completions", content, ct);
            sw.Stop();

            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement;

            var resultContent = root.GetProperty("choices")[0]
                .GetProperty("message").GetProperty("content").GetString() ?? "";

            var usage = root.GetProperty("usage");
            var promptTokens = usage.GetProperty("prompt_tokens").GetInt32();
            var completionTokens = usage.GetProperty("completion_tokens").GetInt32();

            _logger.LogInformation("OpenAI call completed in {Latency}ms. Tokens: prompt={Prompt}, completion={Completion}",
                sw.ElapsedMilliseconds, promptTokens, completionTokens);

            return new AiCompletionResult(resultContent, promptTokens, completionTokens, ProviderName);
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // Claude Provider — Fallback #1 (claude-sonnet-4)
    // ═══════════════════════════════════════════════════════════════════

    public class ClaudeProvider : IAiProvider
    {
        private readonly HttpClient _httpClient;
        private readonly string _model;
        private readonly ILogger<ClaudeProvider> _logger;

        public string ProviderName => "claude-sonnet-4";

        public ClaudeProvider(IConfiguration config, IHttpClientFactory httpClientFactory,
            ILogger<ClaudeProvider> logger)
        {
            _httpClient = httpClientFactory.CreateClient("Claude");
            var apiKey = config["AI:Claude:ApiKey"];
            _model = config["AI:Claude:Model"] ?? "claude-sonnet-4-20250514";
            _logger = logger;

            if (!string.IsNullOrEmpty(apiKey))
            {
                _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);
                _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
            }
            _httpClient.BaseAddress = new Uri("https://api.anthropic.com/v1/");
        }

        public async Task<AiCompletionResult> CompleteAsync(AiPromptRequest request, CancellationToken ct)
        {
            var body = new
            {
                model = _model,
                system = request.SystemPrompt,
                messages = new[]
                {
                    new { role = "user", content = request.UserPrompt }
                },
                max_tokens = request.MaxTokens,
                temperature = (double)request.Temperature
            };

            var json = JsonSerializer.Serialize(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var response = await _httpClient.PostAsync("messages", content, ct);
            sw.Stop();

            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement;

            var resultContent = root.GetProperty("content")[0]
                .GetProperty("text").GetString() ?? "";

            var usage = root.GetProperty("usage");
            var promptTokens = usage.GetProperty("input_tokens").GetInt32();
            var completionTokens = usage.GetProperty("output_tokens").GetInt32();

            _logger.LogInformation("Claude call completed in {Latency}ms. Tokens: prompt={Prompt}, completion={Completion}",
                sw.ElapsedMilliseconds, promptTokens, completionTokens);

            return new AiCompletionResult(resultContent, promptTokens, completionTokens, ProviderName);
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // Gemini Provider — Fallback #2 (gemini-1.5-flash, cheapest)
    // ═══════════════════════════════════════════════════════════════════

    public class GeminiProvider : IAiProvider
    {
        private readonly HttpClient _httpClient;
        private readonly string _model;
        private readonly string? _apiKey;
        private readonly ILogger<GeminiProvider> _logger;

        public string ProviderName => "gemini-1.5-flash";

        public GeminiProvider(IConfiguration config, IHttpClientFactory httpClientFactory,
            ILogger<GeminiProvider> logger)
        {
            _httpClient = httpClientFactory.CreateClient("Gemini");
            _apiKey = config["AI:Gemini:ApiKey"];
            _model = config["AI:Gemini:Model"] ?? "gemini-1.5-flash";
            _logger = logger;
            _httpClient.BaseAddress = new Uri("https://generativelanguage.googleapis.com/v1beta/");
        }

        public async Task<AiCompletionResult> CompleteAsync(AiPromptRequest request, CancellationToken ct)
        {
            var body = new
            {
                system_instruction = new
                {
                    parts = new[] { new { text = request.SystemPrompt } }
                },
                contents = new[]
                {
                    new { role = "user", parts = new[] { new { text = request.UserPrompt } } }
                },
                generationConfig = new
                {
                    maxOutputTokens = request.MaxTokens,
                    temperature = (double)request.Temperature
                }
            };

            var json = JsonSerializer.Serialize(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var url = $"models/{_model}:generateContent?key={_apiKey}";

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var response = await _httpClient.PostAsync(url, content, ct);
            sw.Stop();

            response.EnsureSuccessStatusCode();
            var responseJson = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(responseJson);
            var root = doc.RootElement;

            var resultContent = root.GetProperty("candidates")[0]
                .GetProperty("content").GetProperty("parts")[0]
                .GetProperty("text").GetString() ?? "";

            // Gemini token usage
            int promptTokens = 0, completionTokens = 0;
            if (root.TryGetProperty("usageMetadata", out var usageMeta))
            {
                promptTokens = usageMeta.TryGetProperty("promptTokenCount", out var pt) ? pt.GetInt32() : 0;
                completionTokens = usageMeta.TryGetProperty("candidatesTokenCount", out var ct2) ? ct2.GetInt32() : 0;
            }

            _logger.LogInformation("Gemini call completed in {Latency}ms. Tokens: prompt={Prompt}, completion={Completion}",
                sw.ElapsedMilliseconds, promptTokens, completionTokens);

            return new AiCompletionResult(resultContent, promptTokens, completionTokens, ProviderName);
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // AI Provider Factory — Primary + Fallback Chain
    // ═══════════════════════════════════════════════════════════════════

    public class AiProviderFactory : IAiProviderFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly AiProvider _primaryProvider;
        private readonly ILogger<AiProviderFactory> _logger;

        // Fallback chain: OpenAI → Claude → Gemini
        private static readonly Dictionary<AiProvider, AiProvider> FallbackChain = new()
        {
            { AiProvider.OpenAI, AiProvider.Claude },
            { AiProvider.Claude, AiProvider.Gemini },
            { AiProvider.Gemini, AiProvider.OpenAI }
        };

        public AiProviderFactory(IConfiguration config, IServiceProvider serviceProvider,
            ILogger<AiProviderFactory> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;

            var primaryStr = config["AI:PrimaryProvider"] ?? "OpenAI";
            _primaryProvider = Enum.TryParse<AiProvider>(primaryStr, out var p) ? p : AiProvider.OpenAI;
        }

        public IAiProvider GetProvider(AiProvider preferred)
        {
            return ResolveProvider(preferred);
        }

        public IAiProvider GetFallbackProvider(AiProvider failed)
        {
            if (FallbackChain.TryGetValue(failed, out var fallback))
            {
                _logger.LogWarning("AI provider {Failed} failed. Falling back to {Fallback}.", failed, fallback);
                return ResolveProvider(fallback);
            }
            throw new InvalidOperationException($"No fallback available for provider {failed}");
        }

        private IAiProvider ResolveProvider(AiProvider provider)
        {
            return provider switch
            {
                AiProvider.OpenAI => _serviceProvider.GetRequiredService<OpenAiProvider>(),
                AiProvider.Claude => _serviceProvider.GetRequiredService<ClaudeProvider>(),
                AiProvider.Gemini => _serviceProvider.GetRequiredService<GeminiProvider>(),
                _ => throw new ArgumentOutOfRangeException(nameof(provider))
            };
        }
    }
}
