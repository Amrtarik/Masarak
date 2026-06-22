using Masarak.Application.DTOs;
using Masarak.Application.Interfaces;
using Masarak.Domain.Entities;
using Masarak.Domain.Enums;
using Masarak.Domain.Services;
using Masarak.Domain.Events;
using Masarak.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using MassTransit;

namespace Masarak.Infrastructure.Services
{
    public class AssessmentService : IAssessmentService
    {
        private readonly IAssignmentRepository _assignmentRepo;
        private readonly IExamRepository _examRepo;
        private readonly IStudentExamRepository _studentExamRepo;
        private readonly ISubmissionRepository _submissionRepo;
        private readonly IStudentPerformanceRepository _performanceRepo;
        private readonly ITeachingAssignmentRepository _teachingAssignmentRepo;
        private readonly IStudentClassRepository _studentClassRepo;
        private readonly IFileStorageService _fileStorage;
        private readonly Context _context;
        private readonly AutoGradingService _autoGradingService;
        private readonly ExamTimerEnforcer _timerEnforcer;
        private readonly IPublishEndpoint _publishEndpoint;

        public AssessmentService(
            IAssignmentRepository assignmentRepo,
            IExamRepository examRepo,
            IStudentExamRepository studentExamRepo,
            ISubmissionRepository submissionRepo,
            IStudentPerformanceRepository performanceRepo,
            ITeachingAssignmentRepository teachingAssignmentRepo,
            IStudentClassRepository studentClassRepo,
            IFileStorageService fileStorage,
            Context context,
            IPublishEndpoint publishEndpoint)
        {
            _assignmentRepo = assignmentRepo;
            _examRepo = examRepo;
            _studentExamRepo = studentExamRepo;
            _submissionRepo = submissionRepo;
            _performanceRepo = performanceRepo;
            _teachingAssignmentRepo = teachingAssignmentRepo;
            _studentClassRepo = studentClassRepo;
            _fileStorage = fileStorage;
            _context = context;
            _publishEndpoint = publishEndpoint;
            _autoGradingService = new AutoGradingService();
            _timerEnforcer = new ExamTimerEnforcer();
        }

        // ── Helper ──────────────────────────────────────────────────────────
        private async Task ValidateTeacherOwnsTeachingAssignmentAsync(int teacherUserId, int teachingAssignmentId)
        {
            var ta = await _teachingAssignmentRepo.GetByIdAsync(teachingAssignmentId);
            if (ta == null || ta.Teacher.UserId != teacherUserId)
                throw new UnauthorizedAccessException("You do not have access to this teaching assignment.");
        }

        private async Task ValidateTeacherOwnsAssignmentAsync(int teacherUserId, int assignmentId)
        {
            var assignment = await _assignmentRepo.GetByIdAsync(assignmentId);
            if (assignment == null) throw new KeyNotFoundException("Assignment not found.");
            if (assignment.TeachingAssignment.Teacher.UserId != teacherUserId)
                throw new UnauthorizedAccessException("You do not have access to this assignment.");
        }

        private async Task ValidateTeacherOwnsExamAsync(int teacherUserId, int examId)
        {
            var exam = await _examRepo.GetByIdWithQuestionsAsync(examId);
            if (exam == null) throw new KeyNotFoundException("Exam not found.");
            if (exam.TeachingAssignment.Teacher.UserId != teacherUserId)
                throw new UnauthorizedAccessException("You do not have access to this exam.");
        }

        private async Task<int> GetStudentIdAsync(int userId, CancellationToken ct)
        {
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == userId, ct);
            if (student == null) throw new KeyNotFoundException("Student not found.");
            return student.StudentId;
        }

        // ── Teacher: Assignments ──────────────────────────────────────────────

        public async Task<AssignmentDto> CreateAssignmentAsync(int teacherUserId, CreateAssignmentRequest request, CancellationToken ct = default)
        {
            await ValidateTeacherOwnsTeachingAssignmentAsync(teacherUserId, request.TeachingAssignmentId);

            var assignment = Assignment.Create(
                request.TeachingAssignmentId,
                request.Title,
                request.Instructions,
                request.DueDate,
                request.MaxScore
            );

            await _assignmentRepo.AddAsync(assignment, ct);

            var saved = await _assignmentRepo.GetByIdAsync(assignment.AssignmentId, ct);
            return MapToAssignmentDto(saved!);
        }

        public async Task PublishAssignmentAsync(int teacherUserId, int assignmentId, CancellationToken ct = default)
        {
            await ValidateTeacherOwnsAssignmentAsync(teacherUserId, assignmentId);
            var assignment = await _assignmentRepo.GetByIdAsync(assignmentId, ct);
            assignment!.Publish();
            await _assignmentRepo.UpdateAsync(assignment, ct);
        }

        public async Task CloseAssignmentAsync(int teacherUserId, int assignmentId, CancellationToken ct = default)
        {
            await ValidateTeacherOwnsAssignmentAsync(teacherUserId, assignmentId);
            var assignment = await _assignmentRepo.GetByIdAsync(assignmentId, ct);
            assignment!.Close();
            await _assignmentRepo.UpdateAsync(assignment, ct);
        }

        public async Task<IEnumerable<AssignmentDto>> GetTeacherAssignmentsAsync(int teacherUserId, int teachingAssignmentId, CancellationToken ct = default)
        {
            await ValidateTeacherOwnsTeachingAssignmentAsync(teacherUserId, teachingAssignmentId);
            var assignments = await _assignmentRepo.GetByTeachingAssignmentIdAsync(teachingAssignmentId, ct);
            return assignments.Select(MapToAssignmentDto);
        }

        public async Task<IEnumerable<SubmissionDetailDto>> GetSubmissionsForAssignmentAsync(int teacherUserId, int assignmentId, CancellationToken ct = default)
        {
            await ValidateTeacherOwnsAssignmentAsync(teacherUserId, assignmentId);
            var submissions = await _submissionRepo.GetByAssignmentIdAsync(assignmentId, ct);
            
            return submissions.Select(s => new SubmissionDetailDto(
                s.SubmissionId,
                s.StudentId,
                $"{s.Student.User?.FullName ?? "Unknown"}",
                s.Status,
                s.Score,
                s.SubmittedAt,
                s.FileBlobName != null
            ));
        }

        public async Task<SubmissionDto> GradeSubmissionAsync(int teacherUserId, int submissionId, GradeSubmissionRequest request, CancellationToken ct = default)
        {
            var submission = await _submissionRepo.GetByIdAsync(submissionId, ct);
            if (submission == null) throw new KeyNotFoundException("Submission not found.");
            await ValidateTeacherOwnsAssignmentAsync(teacherUserId, submission.AssignmentId);

            if (request.MarksAwarded > submission.Assignment.MaxScore)
                throw new InvalidOperationException("Marks awarded cannot exceed maximum score.");

            submission.Grade(request.MarksAwarded, request.Feedback);
            await _submissionRepo.UpdateAsync(submission, ct);

            // Fetch missing info for event
            var assignment = await _assignmentRepo.GetByIdAsync(submission.AssignmentId, ct);
            
            await _publishEndpoint.Publish(new AssignmentGradedEvent(
                submission.SubmissionId, 
                submission.Student.UserId, 
                assignment!.TeachingAssignment.SubjectId, 
                assignment!.TeachingAssignment.ClassId), ct);

            return new SubmissionDto(
                submission.SubmissionId,
                submission.AssignmentId,
                submission.Status,
                submission.SubmittedAt,
                submission.Score,
                submission.Feedback,
                null
            );
        }

        public async Task<string> GetSubmissionFileDownloadUrlAsync(int teacherUserId, int submissionId, CancellationToken ct = default)
        {
            var submission = await _submissionRepo.GetByIdAsync(submissionId, ct);
            if (submission == null) throw new KeyNotFoundException("Submission not found.");
            await ValidateTeacherOwnsAssignmentAsync(teacherUserId, submission.AssignmentId);

            if (string.IsNullOrEmpty(submission.FileBlobName))
                throw new InvalidOperationException("No file attached to this submission.");

            return await _fileStorage.GenerateSignedDownloadUrlAsync(submission.FileBlobName, "assignments", TimeSpan.FromMinutes(60), ct);
        }

        // ── Teacher: Exams ────────────────────────────────────────────────────

        public async Task<ExamDto> CreateExamAsync(int teacherUserId, CreateExamRequest request, CancellationToken ct = default)
        {
            await ValidateTeacherOwnsTeachingAssignmentAsync(teacherUserId, request.TeachingAssignmentId);

            var exam = Exam.Create(
                request.TeachingAssignmentId,
                request.Title,
                request.Instructions,
                request.StartTime,
                request.EndTime,
                request.DurationMinutes
            );

            await _examRepo.AddAsync(exam, ct);
            return MapToExamDto(exam);
        }

        public async Task<QuestionDto> AddQuestionToExamAsync(int teacherUserId, int examId, AddQuestionRequest request, CancellationToken ct = default)
        {
            var exam = await _examRepo.GetByIdWithQuestionsAsync(examId, ct);
            if (exam == null) throw new KeyNotFoundException("Exam not found.");
            if (exam.TeachingAssignment.Teacher.UserId != teacherUserId)
                throw new UnauthorizedAccessException();
            if (exam.Status != ExamStatus.Draft)
                throw new InvalidOperationException("Cannot add questions to a published/closed exam.");

            var question = new Question
            {
                ExamId = examId,
                Type = request.Type,
                QuestionText = request.Text,
                Marks = request.Marks,
                Difficulty = request.Difficulty,
                OrderNum = request.Order,
                CorrectAns = request.CorrectAnswer,
                ImageUrl = request.ImageUrl
            };

            if (request.Options != null)
            {
                foreach (var opt in request.Options)
                {
                    question.Options.Add(new QuestionOption
                    {
                        Label = opt.Label.Length > 0 ? opt.Label[0] : 'A',
                        Text = opt.Text
                    });
                }
            }

            exam.Questions.Add(question);
            exam.RecalculateTotalMarks();
            await _examRepo.UpdateAsync(exam, ct);

            return MapToQuestionDto(question, includeCorrectAnswer: true);
        }

        public async Task<QuestionDto> UpdateQuestionAsync(int teacherUserId, int questionId, UpdateQuestionRequest request, CancellationToken ct = default)
        {
            var exam = await _context.Exams.Include(e => e.TeachingAssignment).ThenInclude(ta => ta.Teacher)
                                           .Include(e => e.Questions).ThenInclude(q => q.Options)
                                           .FirstOrDefaultAsync(e => e.Questions.Any(q => q.QuestionId == questionId), ct);
            if (exam == null) throw new KeyNotFoundException("Question or Exam not found.");
            if (exam.TeachingAssignment.Teacher.UserId != teacherUserId) throw new UnauthorizedAccessException();
            if (exam.Status != ExamStatus.Draft) throw new InvalidOperationException("Cannot modify published exam.");

            var question = exam.Questions.First(q => q.QuestionId == questionId);
            question.Type = request.Type;
            question.QuestionText = request.Text;
            question.Marks = request.Marks;
            question.Difficulty = request.Difficulty;
            question.OrderNum = request.Order;
            question.CorrectAns = request.CorrectAnswer;
            question.ImageUrl = request.ImageUrl;

            question.Options.Clear();
            if (request.Options != null)
            {
                foreach (var opt in request.Options)
                {
                    question.Options.Add(new QuestionOption { Label = opt.Label.Length > 0 ? opt.Label[0] : 'A', Text = opt.Text });
                }
            }
            exam.RecalculateTotalMarks();
            await _examRepo.UpdateAsync(exam, ct);
            return MapToQuestionDto(question, true);
        }

        public async Task RemoveQuestionAsync(int teacherUserId, int questionId, CancellationToken ct = default)
        {
            var exam = await _context.Exams.Include(e => e.TeachingAssignment).ThenInclude(ta => ta.Teacher)
                                           .Include(e => e.Questions)
                                           .FirstOrDefaultAsync(e => e.Questions.Any(q => q.QuestionId == questionId), ct);
            if (exam == null) throw new KeyNotFoundException("Question or Exam not found.");
            if (exam.TeachingAssignment.Teacher.UserId != teacherUserId) throw new UnauthorizedAccessException();
            if (exam.Status != ExamStatus.Draft) throw new InvalidOperationException("Cannot modify published exam.");

            var question = exam.Questions.First(q => q.QuestionId == questionId);
            exam.Questions.Remove(question);
            exam.RecalculateTotalMarks();
            await _examRepo.UpdateAsync(exam, ct);
        }

        public async Task PublishExamAsync(int teacherUserId, int examId, CancellationToken ct = default)
        {
            var exam = await _examRepo.GetByIdWithQuestionsAsync(examId, ct);
            if (exam == null) throw new KeyNotFoundException("Exam not found.");
            if (exam.TeachingAssignment.Teacher.UserId != teacherUserId)
                throw new UnauthorizedAccessException();

            exam.Publish();
            await _examRepo.UpdateAsync(exam, ct);
        }

        public async Task<IEnumerable<ExamDto>> GetTeacherExamsAsync(int teacherUserId, int teachingAssignmentId, CancellationToken ct = default)
        {
            await ValidateTeacherOwnsTeachingAssignmentAsync(teacherUserId, teachingAssignmentId);
            var exams = await _examRepo.GetByTeachingAssignmentIdAsync(teachingAssignmentId, ct);
            return exams.Select(MapToExamDto);
        }

        // ── Teacher: Grading Dashboard ────────────────────────────────────────

        public async Task<PendingGradingDashboardDto> GetPendingGradingDashboardAsync(int teacherUserId, CancellationToken ct = default)
        {
            var pendingExams = await _studentExamRepo.GetPendingManualGradingForTeacherAsync(teacherUserId, ct);
            
            int totalExams = pendingExams.Sum(e => e.StudentAnswers.Count(a => a.GradingStatus == AnswerGradingStatus.PendingReview));
            var examSummaries = pendingExams.GroupBy(e => e.ExamId)
                .Select(g => new PendingExamSummaryDto(
                    g.Key, 
                    g.First().Exam.Title, 
                    g.Sum(e => e.StudentAnswers.Count(a => a.GradingStatus == AnswerGradingStatus.PendingReview))
                ));

            // Similarly for assignments
            var pendingAssignments = await _context.Submissions
                .Include(s => s.Assignment).ThenInclude(a => a.TeachingAssignment)
                .Where(s => s.Assignment.TeachingAssignment.Teacher.UserId == teacherUserId && s.Status == SubmissionStatus.Submitted)
                .ToListAsync(ct);

            int totalSubs = pendingAssignments.Count;
            var subSummaries = pendingAssignments.GroupBy(s => s.AssignmentId)
                .Select(g => new PendingAssignmentSummaryDto(g.Key, g.First().Assignment.Title, g.Count()));

            return new PendingGradingDashboardDto(totalExams, totalSubs, examSummaries, subSummaries);
        }

        public async Task<ExamGradingReviewDto> GetStudentAnswersForReviewAsync(int teacherUserId, int studentExamId, CancellationToken ct = default)
        {
            var studentExam = await _studentExamRepo.GetByIdWithAnswersAsync(studentExamId, ct);
            if (studentExam == null) throw new KeyNotFoundException("Student Exam not found.");
            await ValidateTeacherOwnsExamAsync(teacherUserId, studentExam.ExamId);

            var pendingAnswers = studentExam.StudentAnswers
                .Where(a => a.GradingStatus == AnswerGradingStatus.PendingReview)
                .Select(a => new PendingAnswerDto(
                    a.AnswerId, a.QuestionId, a.Question.QuestionText, a.Question.Marks, a.AnswerText, a.FileUrl
                ));

            return new ExamGradingReviewDto(
                studentExam.StudentExamId, 
                studentExam.Student.User?.FullName ?? "Unknown", 
                studentExam.Exam.Title, 
                studentExam.TotalAutoScore ?? 0, 
                pendingAnswers
            );
        }

        public async Task GradeStudentAnswerAsync(int teacherUserId, int answerId, GradeStudentAnswerRequest request, CancellationToken ct = default)
        {
            var answer = await _context.StudentAnswers
                .Include(a => a.StudentExam).ThenInclude(se => se.Exam).ThenInclude(e => e.TeachingAssignment).ThenInclude(ta => ta.Teacher)
                .Include(a => a.Question)
                .FirstOrDefaultAsync(a => a.AnswerId == answerId, ct);

            if (answer == null) throw new KeyNotFoundException();
            if (answer.StudentExam.Exam.TeachingAssignment.Teacher.UserId != teacherUserId) throw new UnauthorizedAccessException();

            if (request.MarksAwarded > answer.Question.Marks) throw new InvalidOperationException("Marks exceed max marks.");

            answer.ManualGrade(request.MarksAwarded, request.Feedback);
            
            var examAnswers = await _context.StudentAnswers.Where(a => a.StudentExamId == answer.StudentExamId).ToListAsync(ct);
            if (!examAnswers.Any(a => a.GradingStatus == AnswerGradingStatus.PendingReview))
            {
                var manualScore = examAnswers.Where(a => a.GradingStatus == AnswerGradingStatus.ManuallyGraded).Sum(a => a.MarksAwarded ?? 0);
                answer.StudentExam.FinalizeMixedGrading(manualScore);
                
                await _publishEndpoint.Publish(new ExamFullyGradedEvent(
                    answer.StudentExamId, 
                    answer.StudentExam.Student.UserId, 
                    answer.StudentExam.Exam.TeachingAssignment.SubjectId, 
                    answer.StudentExam.Exam.TeachingAssignment.ClassId), ct);
            }

            await _context.SaveChangesAsync(ct);
        }

        // ── Student: Assignments ──────────────────────────────────────────────

        public async Task<IEnumerable<AssignmentDto>> GetStudentAssignmentsAsync(int studentUserId, int subjectId, CancellationToken ct = default)
        {
            var studentId = await GetStudentIdAsync(studentUserId, ct);
            var studentClass = await _context.StudentClasses.FirstOrDefaultAsync(sc => sc.StudentId == studentId && sc.IsActive, ct);
            if (studentClass == null) return Enumerable.Empty<AssignmentDto>();

            var assignments = await _assignmentRepo.GetPublishedForClassAsync(studentClass.ClassId, subjectId, ct);
            return assignments.Select(MapToAssignmentDto);
        }

        public async Task<SubmissionDto> SubmitAssignmentAsync(int studentUserId, int assignmentId, string? textContent, Stream? fileStream, string? fileName, string? contentType, CancellationToken ct = default)
        {
            var studentId = await GetStudentIdAsync(studentUserId, ct);
            var existing = await _submissionRepo.GetByStudentAndAssignmentAsync(studentId, assignmentId, ct);
            if (existing != null) throw new InvalidOperationException("Assignment already submitted.");

            var assignment = await _assignmentRepo.GetByIdAsync(assignmentId, ct);
            if (assignment == null || assignment.Status != AssignmentStatus.Published) throw new InvalidOperationException("Assignment not available.");

            string? fileUrl = null;
            string? blobName = null;
            if (fileStream != null && fileName != null)
            {
                var upload = await _fileStorage.UploadAsync(fileStream, fileName, "assignments", ct);
                fileUrl = upload.PublicUrl;
                blobName = upload.BlobName;
            }

            var submission = Submission.Create(assignmentId, studentId, textContent, fileUrl, blobName);
            await _submissionRepo.AddAsync(submission, ct);

            return new SubmissionDto(submission.SubmissionId, submission.AssignmentId, submission.Status, submission.SubmittedAt, submission.Score, submission.Feedback, fileUrl);
        }

        // ── Student: Exams ────────────────────────────────────────────────────

        public async Task<IEnumerable<ExamDto>> GetOpenExamsForStudentAsync(int studentUserId, int subjectId, CancellationToken ct = default)
        {
            var studentId = await GetStudentIdAsync(studentUserId, ct);
            var studentClass = await _context.StudentClasses.FirstOrDefaultAsync(sc => sc.StudentId == studentId && sc.IsActive, ct);
            if (studentClass == null) return Enumerable.Empty<ExamDto>();

            var exams = await _examRepo.GetOpenExamsForClassAsync(studentClass.ClassId, subjectId, DateTime.UtcNow, ct);
            return exams.Select(MapToExamDto);
        }

        public async Task<ExamAttemptDto> StartExamAttemptAsync(int studentUserId, int examId, CancellationToken ct = default)
        {
            var studentId = await GetStudentIdAsync(studentUserId, ct);
            var exam = await _examRepo.GetByIdWithQuestionsAsync(examId, ct);
            if (exam == null || exam.Status != ExamStatus.Published) throw new InvalidOperationException("Exam unavailable.");

            var now = DateTime.UtcNow;
            if (now < exam.StartTime || now > exam.EndTime) throw new InvalidOperationException("Outside exam window.");

            var attempt = await _studentExamRepo.GetByStudentAndExamAsync(studentId, examId, ct);
            if (attempt == null)
            {
                attempt = StudentExam.Begin(examId, studentId, exam.DurationMins);
                await _studentExamRepo.AddAsync(attempt, ct);
            }

            if (!_timerEnforcer.IsWithinTimeWindow(attempt))
                throw new InvalidOperationException("Exam time expired.");

            return new ExamAttemptDto(
                attempt.StudentExamId, exam.ExamId, exam.Title, attempt.ExpiresAt,
                (int)(attempt.ExpiresAt - DateTime.UtcNow).TotalSeconds,
                exam.Questions.Select(q => MapToQuestionDto(q, false)),
                attempt.StudentAnswers.Select(a => new SavedAnswerDto(a.QuestionId, a.AnswerText, a.SelectedOptionId))
            );
        }

        public async Task SaveAnswerAsync(int studentUserId, int studentExamId, SaveAnswerRequest request, Stream? fileStream, string? fileName, string? contentType, CancellationToken ct = default)
        {
            var studentId = await GetStudentIdAsync(studentUserId, ct);
            var attempt = await _studentExamRepo.GetByIdWithAnswersAsync(studentExamId, ct);
            if (attempt == null || attempt.StudentId != studentId) throw new UnauthorizedAccessException();

            if (attempt.Status != StudentExamStatus.InProgress || !_timerEnforcer.IsWithinTimeWindow(attempt))
                throw new InvalidOperationException("Exam is closed or expired.");

            var answer = attempt.StudentAnswers.FirstOrDefault(a => a.QuestionId == request.QuestionId);
            if (answer == null)
            {
                answer = new StudentAnswer { StudentExamId = studentExamId, QuestionId = request.QuestionId };
                attempt.StudentAnswers.Add(answer);
            }

            answer.AnswerText = request.AnswerText;
            answer.SelectedOptionId = request.SelectedOptionId;

            if (fileStream != null && fileName != null)
            {
                var upload = await _fileStorage.UploadAsync(fileStream, fileName, "exam-answers", ct);
                answer.FileUrl = upload.PublicUrl;
                answer.FileBlobName = upload.BlobName;
            }

            await _studentExamRepo.UpdateAsync(attempt, ct);
        }

        public async Task<ExamResultDto> SubmitExamAsync(int studentUserId, int studentExamId, CancellationToken ct = default)
        {
            var studentId = await GetStudentIdAsync(studentUserId, ct);
            var attempt = await _studentExamRepo.GetByIdWithAnswersAsync(studentExamId, ct);
            if (attempt == null || attempt.StudentId != studentId) throw new UnauthorizedAccessException();

            if (attempt.Status != StudentExamStatus.InProgress)
                throw new InvalidOperationException("Exam already submitted.");

            attempt.MarkSubmitted();

            decimal totalAutoScore = 0;
            bool needsManualGrading = false;
            foreach(var ans in attempt.StudentAnswers)
            {
                if (ans.Question.IsAutoGraded)
                {
                    totalAutoScore += _autoGradingService.Grade(ans.Question, ans);
                }
                else
                {
                    needsManualGrading = true;
                }
            }

            attempt.ApplyAutoGrading(totalAutoScore);
            if (!needsManualGrading)
            {
                attempt.FinalizeFullAutoGrading(totalAutoScore);
                var exam = await _examRepo.GetByIdWithQuestionsAsync(attempt.ExamId, ct);
                await _publishEndpoint.Publish(new ExamFullyGradedEvent(
                    attempt.StudentExamId, 
                    attempt.Student.UserId, 
                    exam!.TeachingAssignment.SubjectId, 
                    exam!.TeachingAssignment.ClassId), ct);
            }
            else
            {
                attempt.HasPendingManualGrading = true;
            }

            await _studentExamRepo.UpdateAsync(attempt, ct);
            return await GetStudentExamResultAsync(studentUserId, studentExamId, ct);
        }

        public async Task<ExamResultDto> GetStudentExamResultAsync(int studentUserId, int studentExamId, CancellationToken ct = default)
        {
            var studentId = await GetStudentIdAsync(studentUserId, ct);
            var attempt = await _studentExamRepo.GetByIdWithAnswersAsync(studentExamId, ct);
            if (attempt == null || attempt.StudentId != studentId) throw new UnauthorizedAccessException();

            return new ExamResultDto(
                attempt.StudentExamId, attempt.FinalScore ?? 0, attempt.Exam.TotalMarks,
                attempt.Exam.TotalMarks > 0 ? ((attempt.FinalScore ?? 0) / attempt.Exam.TotalMarks) * 100 : 0,
                attempt.HasPendingManualGrading,
                attempt.StudentAnswers.Select(a => new AnswerResultDto(
                    a.QuestionId, a.Question.QuestionText, a.AnswerText ?? a.SelectedOptionId,
                    attempt.Status == StudentExamStatus.Graded ? a.Question.CorrectAns : null,
                    a.MarksAwarded ?? 0, a.Question.Marks, a.GradingStatus, a.TeacherFeedback
                ))
            );
        }

        // ── Performance & Reports ─────────────────────────────────────────────

        public async Task<IEnumerable<SubjectPerformanceDto>> GetStudentPerformanceAsync(int studentUserId, string academicYear, CancellationToken ct = default)
        {
            var studentId = await GetStudentIdAsync(studentUserId, ct);
            var perfs = await _performanceRepo.GetByStudentAndYearAsync(studentId, academicYear, ct);
            return perfs.Select(p => new SubjectPerformanceDto(
                p.SubjectId, p.Subject.Name, p.AvgExam, p.AvgAssignment, p.TotalExamsTaken, p.TotalAssignmentsSubmitted
            ));
        }

        public async Task<ClassPerformanceReportDto> GetClassPerformanceReportAsync(int classId, int subjectId, string academicYear, CancellationToken ct = default)
        {
            var perfs = await _performanceRepo.GetByClassAndSubjectAsync(classId, subjectId, academicYear, ct);
            
            var subjectName = await _context.Subjects.Where(s => s.SubjectId == subjectId).Select(s => s.Name).FirstOrDefaultAsync(ct) ?? "Unknown";
            var className = await _context.Classes.Where(c => c.ClassId == classId).Select(c => c.Name).FirstOrDefaultAsync(ct) ?? "Unknown";

            var studentScores = perfs.Select(p => new StudentScoreDto(p.StudentId, p.Student.User?.FullName ?? "Unknown", p.AvgExam * 0.7m + p.AvgAssignment * 0.3m));
            decimal classAvg = studentScores.Any() ? studentScores.Average(s => s.FinalScore) : 0;

            return new ClassPerformanceReportDto(classId, className, subjectName, classAvg, studentScores);
        }

        // ── Mappers ───────────────────────────────────────────────────────────
        
        private AssignmentDto MapToAssignmentDto(Assignment a)
        {
            return new AssignmentDto(
                a.AssignmentId,
                a.Title,
                a.DueDate,
                a.MaxScore,
                a.Status,
                a.TeachingAssignment?.Subject?.Name ?? "Unknown",
                a.TeachingAssignment?.Class?.Name ?? "Unknown",
                a.Submissions?.Count ?? 0
            );
        }

        private ExamDto MapToExamDto(Exam e)
        {
            return new ExamDto(
                e.ExamId,
                e.Title,
                e.StartTime,
                e.EndTime,
                e.DurationMins,
                e.TotalMarks,
                e.Status,
                e.Questions?.Count ?? 0
            );
        }

        private QuestionDto MapToQuestionDto(Question q, bool includeCorrectAnswer)
        {
            return new QuestionDto(
                q.QuestionId,
                q.Type,
                q.QuestionText,
                q.Marks,
                q.Difficulty,
                q.OrderNum,
                q.Options?.Select(o => new QuestionOptionDto(o.Label.ToString(), o.Text))
            );
        }
    }
}
