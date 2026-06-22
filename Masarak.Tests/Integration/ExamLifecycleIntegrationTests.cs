using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Masarak.Application.DTOs;
using Masarak.Application.Interfaces;
using Masarak.Domain.Entities;
using Masarak.Domain.Enums;
using Masarak.Infrastructure.Persistence;
using Masarak.Infrastructure.Persistence.Repositories;
using Masarak.Infrastructure.Services;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Masarak.Tests.Integration
{
    public class ExamLifecycleIntegrationTests
    {
        private readonly Context _context;
        private readonly AssessmentService _assessmentService;
        private readonly Mock<IPublishEndpoint> _mockPublishEndpoint;
        private readonly Mock<IFileStorageService> _mockFileStorage;

        public ExamLifecycleIntegrationTests()
        {
            var options = new DbContextOptionsBuilder<Context>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new Context(options);

            _mockPublishEndpoint = new Mock<IPublishEndpoint>();
            _mockFileStorage = new Mock<IFileStorageService>();

            var assignmentRepo = new AssignmentRepository(_context);
            var examRepo = new ExamRepository(_context);
            var studentExamRepo = new StudentExamRepository(_context);
            var submissionRepo = new SubmissionRepository(_context);
            var perfRepo = new StudentPerformanceRepository(_context);
            
            // We use Mocks for the related cross-domain repositories if we want, or just concrete since we have InMemory DB.
            var taRepo = new Mock<ITeachingAssignmentRepository>();
            var scRepo = new Mock<IStudentClassRepository>();

            _assessmentService = new AssessmentService(
                assignmentRepo, examRepo, studentExamRepo, submissionRepo,
                perfRepo, taRepo.Object, scRepo.Object, _mockFileStorage.Object,
                _context, _mockPublishEndpoint.Object
            );
        }

        [Fact]
        public async Task FullExamLifecycle_AutoGraded_PublishesEventAndReturnsCorrectScore()
        {
            // Arrange
            // 1. Seed Student, Teacher, Subject, Class, TeachingAssignment
            var user = new User { UserId = 1, RoleId = 3, FullName = "Student One", Email = "s@a.com", PasswordHash = "x" };
            var student = new Student { StudentId = 10, UserId = 1, AcademicStatus = "Active" };
            
            var teacherUser = new User { UserId = 2, RoleId = 2, FullName = "Teacher One", Email = "t@a.com", PasswordHash = "x" };
            var teacher = new Teacher { TeacherId = 20, UserId = 2 };

            var subject = new Subject { SubjectId = 30, Name = "Math" };
            var @class = new Class { ClassId = 40, Name = "10A" };

            var teachingAssignment = new TeachingAssignment { AssignmentId = 50, TeacherId = 20, SubjectId = 30, ClassId = 40, IsActive = true, Teacher = teacher };

            var exam = new Exam { 
                ExamId = 100, 
                AssignmentId = 50, 
                Title = "Math Midterm", 
                StartTime = DateTime.UtcNow.AddHours(-1), 
                EndTime = DateTime.UtcNow.AddHours(1),
                DurationMins = 60,
                Status = ExamStatus.Published,
                TeachingAssignment = teachingAssignment
            };

            var q1 = new Question { QuestionId = 1001, ExamId = 100, Type = QuestionType.MCQ, Marks = 10, CorrectAns = "A", QuestionText = "1+1" };
            q1.Options.Add(new QuestionOption { Label = 'A', Text = "2" });
            exam.Questions.Add(q1);

            var q2 = new Question { QuestionId = 1002, ExamId = 100, Type = QuestionType.TrueFalse, Marks = 5, CorrectAns = "True", QuestionText = "Earth is round" };
            exam.Questions.Add(q2);

            exam.RecalculateTotalMarks();

            _context.Users.AddRange(user, teacherUser);
            _context.Students.Add(student);
            _context.Teachers.Add(teacher);
            _context.Subjects.Add(subject);
            _context.Classes.Add(@class);
            _context.TeachingAssignments.Add(teachingAssignment);
            _context.Exams.Add(exam);
            await _context.SaveChangesAsync();

            // Act
            // Student Starts Exam
            var attemptResult = await _assessmentService.StartExamAttemptAsync(user.UserId, exam.ExamId);
            Assert.NotNull(attemptResult);
            Assert.Equal(2, attemptResult.Questions.Count());

            // Student Saves Answer to Q1 (Correct)
            await _assessmentService.SaveAnswerAsync(user.UserId, attemptResult.StudentExamId, new SaveAnswerRequest { QuestionId = 1001, SelectedOptionId = "A" }, null, null, null);
            
            // Student Saves Answer to Q2 (Incorrect)
            await _assessmentService.SaveAnswerAsync(user.UserId, attemptResult.StudentExamId, new SaveAnswerRequest { QuestionId = 1002, AnswerText = "False" }, null, null, null);

            // Student Submits Exam
            var submitResult = await _assessmentService.SubmitExamAsync(user.UserId, attemptResult.StudentExamId);

            // Assert
            Assert.False(submitResult.HasPendingManualGrading);
            Assert.Equal(10, submitResult.FinalScore); // Q1 correct (10) + Q2 incorrect (0)
            Assert.Equal(15, submitResult.TotalMarks);
            Assert.Equal(2, submitResult.Answers.Count());

            // Verify event published
            _mockPublishEndpoint.Verify(
                p => p.Publish(It.IsAny<Masarak.Domain.Events.ExamFullyGradedEvent>(), It.IsAny<CancellationToken>()), 
                Times.Once);
        }
    }
}
