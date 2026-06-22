using Masarak.Domain.Entities;
using Masarak.Domain.Enums;

namespace Masarak.Domain.Services
{
    /// <summary>
    /// Evaluates a StudentAnswer against its Question's CorrectAnswer for auto-gradeable types.
    /// MCQ: string equality on selected option label
    /// TrueFalse: "True" / "False" equality (case-insensitive)
    /// FillInBlank: normalized string equality (trim, lowercase, ignore punctuation)
    /// Returns marks awarded (full marks or 0 — no partial for auto-graded).
    /// </summary>
    public class AutoGradingService
    {
        public decimal Grade(Question question, StudentAnswer answer)
        {
            if (!question.IsAutoGraded || string.IsNullOrWhiteSpace(question.CorrectAns))
                return 0;

            string correctAnswer = question.CorrectAns;
            answer.AutoGrade(correctAnswer, question.Marks);

            return answer.MarksAwarded ?? 0;
        }
    }
}
