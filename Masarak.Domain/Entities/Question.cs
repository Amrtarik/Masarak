using Masarak.Domain.Enums;

namespace Masarak.Domain.Entities
{
    /// <summary>
    /// A question belonging to an Exam, or a standalone question bank entry.
    ///
    /// Phase 3 additions:
    ///   • QuestionType enum (MCQ, TrueFalse, ShortAnswer, Essay, FillInBlank, FileUpload)
    ///   • DifficultyLevel enum
    ///   • ImageUrl, SubjectId, QuestionBankId
    ///   • QuestionOption owned collection replaces JSON Options
    ///   • IsAutoGraded computed property
    /// </summary>
    public class Question
    {
        public int              QuestionId   { get; set; }
        public int?             ExamId       { get; set; }             // null if question bank only
        public int?             QuestionBankId { get; set; }           // source bank entry
        public int?             SubjectId    { get; set; }             // FK → subjects.SubjectId
        public QuestionType     Type         { get; set; }
        public string           QuestionText { get; set; } = null!;
        public string?          ImageUrl     { get; set; }
        public decimal          Marks        { get; set; } = 1;
        public DifficultyLevel  Difficulty   { get; set; } = DifficultyLevel.Medium;
        public int              OrderNum     { get; set; } = 1;
        public string?          CorrectAns   { get; set; }             // MCQ: option label; T/F: "True"/"False"; FIB: exact string

        /// <summary>True for question types that can be auto-graded (MCQ, TrueFalse, FillInBlank).</summary>
        public bool IsAutoGraded => Type is QuestionType.MCQ or QuestionType.TrueFalse or QuestionType.FillInBlank;

        // ── Navigation ──────────────────────────────────────────────────────
        public virtual Exam? Exam { get; set; }
        public virtual ICollection<QuestionOption> Options       { get; set; } = new List<QuestionOption>();
        public virtual ICollection<StudentAnswer>  StudentAnswers { get; set; } = new List<StudentAnswer>();
    }
}

