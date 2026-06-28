using Masarak.Domain.Enums;

namespace Masarak.Domain.Entities
{
    /// <summary>
    /// AI-generated recommendation stored with provider metadata and TTL.
    ///
    /// Phase 5 refactored: Now tracks recommendation type, AI provider used,
    /// token consumption for cost monitoring, and soft TTL for cache invalidation.
    ///
    /// Auth integration:
    ///   Students read their own recommendations (StudentOnly).
    ///   Parents read recommendations for linked children (ParentOnly).
    ///   Teachers read per-student insights for their assigned students.
    ///   Generation triggered by RabbitMQ consumers or on-demand requests.
    /// </summary>
    public class AiRecommendation
    {
        public int                AiRecommendationId  { get; set; }
        public int                StudentUserId       { get; set; }   // FK → users.UserId
        public int?               SubjectId           { get; set; }   // FK → subjects.SubjectId
        public RecommendationType Type                { get; set; }
        public string             Payload             { get; set; } = null!;  // JSON: structured recommendation data
        public string?            ProviderUsed        { get; set; }   // "openai-gpt-4o", "claude-sonnet-4", "gemini-1.5-flash"
        public int?               PromptTokensUsed    { get; set; }
        public int?               CompletionTokensUsed { get; set; }
        public DateTime           GeneratedAt         { get; set; }
        public DateTime           ExpiresAt           { get; set; }   // soft TTL: if past, regenerate on next request
        public bool               IsActive            { get; set; } = true;

        // ── Navigation ──────────────────────────────────────────────────────
        public virtual User     Student { get; set; } = null!;
        public virtual Subject? Subject { get; set; }

        // ── Domain Methods ──────────────────────────────────────────────────
        public bool IsExpired() => DateTime.UtcNow > ExpiresAt;

        public static AiRecommendation Create(int studentUserId, int? subjectId,
            RecommendationType type, string payload, string provider,
            int promptTokens, int completionTokens, int cacheTtlHours)
        {
            return new AiRecommendation
            {
                StudentUserId = studentUserId,
                SubjectId = subjectId,
                Type = type,
                Payload = payload,
                ProviderUsed = provider,
                PromptTokensUsed = promptTokens,
                CompletionTokensUsed = completionTokens,
                GeneratedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddHours(cacheTtlHours),
                IsActive = true
            };
        }

        public void Deactivate()
        {
            IsActive = false;
        }
    }
}
