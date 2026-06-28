using Masarak.Application.DTOs;
using Masarak.Domain.Enums;

namespace Masarak.Application.Interfaces
{
    /// <summary>
    /// Abstraction over an AI completion provider (OpenAI, Claude, Gemini).
    /// </summary>
    public interface IAiProvider
    {
        string ProviderName { get; }
        Task<AiCompletionResult> CompleteAsync(AiPromptRequest request, CancellationToken ct);
    }

    /// <summary>
    /// Factory that returns the configured primary AI provider and falls back on failure.
    /// </summary>
    public interface IAiProviderFactory
    {
        IAiProvider GetProvider(AiProvider preferred);
        IAiProvider GetFallbackProvider(AiProvider failed);
    }
}
