namespace Masarak.Domain.Entities
{
    /// <summary>
    /// Admin-editable prompt template for AI generation.
    /// Keys: "weakness_analysis", "parent_report", "teaching_suggestion"
    ///
    /// Templates contain {placeholders} substituted at runtime with student data.
    /// Admin can edit via PUT /api/admin/ai/prompt-templates/{key} without redeployment.
    /// </summary>
    public class AiPromptTemplate
    {
        public int      AiPromptTemplateId  { get; set; }
        public string   Key                 { get; set; } = null!;  // unique key
        public string   SystemPrompt        { get; set; } = null!;
        public string   UserPromptTemplate  { get; set; } = null!;  // with {placeholders}
        public int      MaxTokens           { get; set; }
        public decimal  Temperature         { get; set; }
        public DateTime UpdatedAt           { get; set; }
        public string   UpdatedBy           { get; set; } = null!;

        // ── Domain Methods ──────────────────────────────────────────────────
        public void Update(string systemPrompt, string userPromptTemplate, 
            int maxTokens, decimal temperature, string updatedBy)
        {
            SystemPrompt = systemPrompt;
            UserPromptTemplate = userPromptTemplate;
            MaxTokens = maxTokens;
            Temperature = temperature;
            UpdatedBy = updatedBy;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
