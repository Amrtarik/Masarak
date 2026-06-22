using Masarak.Domain.Entities;
using Masarak.Domain.Enums;
using Masarak.Domain.Services;
using Xunit;

namespace Masarak.Tests.Domain
{
    public class AutoGradingServiceTests
    {
        private readonly AutoGradingService _service;

        public AutoGradingServiceTests()
        {
            _service = new AutoGradingService();
        }

        [Fact]
        public void Grade_MCQ_CorrectAnswer_ReturnsFullMarks()
        {
            // Arrange
            var question = new Question { Type = QuestionType.MCQ, Marks = 5, CorrectAns = "A" };
            var answer = new StudentAnswer { SelectedOptionId = "A", Question = question };

            // Act
            var score = _service.Grade(question, answer);

            // Assert
            Assert.Equal(5, score);
        }

        [Fact]
        public void Grade_MCQ_IncorrectAnswer_ReturnsZero()
        {
            // Arrange
            var question = new Question { Type = QuestionType.MCQ, Marks = 5, CorrectAns = "A" };
            var answer = new StudentAnswer { SelectedOptionId = "B", Question = question };

            // Act
            var score = _service.Grade(question, answer);

            // Assert
            Assert.Equal(0, score);
        }

        [Theory]
        [InlineData("True", "True", 10)]
        [InlineData("true", "True", 10)]
        [InlineData("False", "True", 0)]
        public void Grade_TrueFalse_ReturnsExpectedMarks(string studentAnswer, string correctAnswer, int expectedMarks)
        {
            // Arrange
            var question = new Question { Type = QuestionType.TrueFalse, Marks = 10, CorrectAns = correctAnswer };
            var answer = new StudentAnswer { AnswerText = studentAnswer, Question = question };

            // Act
            var score = _service.Grade(question, answer);

            // Assert
            Assert.Equal(expectedMarks, score);
        }

        [Theory]
        [InlineData("photosynthesis", "photosynthesis", 2)]
        [InlineData(" Photosynthesis ", "photosynthesis", 2)] // Whitespace padding
        [InlineData("photo synthesis", "photosynthesis", 0)] // Strict equality required (currently)
        [InlineData("respiration", "photosynthesis", 0)]
        public void Grade_FillInBlank_ReturnsExpectedMarks(string studentAnswer, string correctAnswer, int expectedMarks)
        {
            // Arrange
            var question = new Question { Type = QuestionType.FillInBlank, Marks = 2, CorrectAns = correctAnswer };
            var answer = new StudentAnswer { AnswerText = studentAnswer, Question = question };

            // Act
            var score = _service.Grade(question, answer);

            // Assert
            Assert.Equal(expectedMarks, score);
        }

        [Fact]
        public void Grade_SubjectiveQuestion_ReturnsZero()
        {
            // Arrange
            var question = new Question { Type = QuestionType.Essay, Marks = 10 };
            var answer = new StudentAnswer { AnswerText = "Blah blah", Question = question };

            // Act
            var score = _service.Grade(question, answer);

            // Assert
            Assert.Equal(0, score);
        }
    }
}
