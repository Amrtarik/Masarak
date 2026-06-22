namespace Masarak.Domain.Entities
{
    /// <summary>
    /// An individual option for an MCQ or similar question type.
    /// Stores label ('A', 'B', 'C', 'D') and text.
    /// </summary>
    public class QuestionOption
    {
        public int    QuestionOptionId { get; set; }
        public int    QuestionId       { get; set; }   // FK → questions.QuestionId
        public string Text             { get; set; } = null!;
        public char   Label            { get; set; }   // 'A', 'B', 'C', 'D'

        // ── Navigation ──────────────────────────────────────────────────────
        public virtual Question Question { get; set; } = null!;
    }
}
