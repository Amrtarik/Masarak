using Masarak.Application.DTOs;
using Masarak.Application.Interfaces;
using Masarak.Domain.Entities;
using Masarak.Domain.Enums;
using Masarak.Domain.Events;
using Masarak.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Masarak.Infrastructure.Services.AI
{
    public class AiAnalyticsService : IAiAnalyticsService
    {
        private readonly Context _context;
        private readonly IAiRecommendationRepository _recRepo;
        private readonly IPerformanceAlertRepository _alertRepo;
        private readonly IAiPromptTemplateRepository _templateRepo;
        private readonly IAnalyticsSnapshotRepository _snapshotRepo;
        private readonly IAiProviderFactory _providerFactory;
        private readonly IDistributedCache _cache;
        private readonly IBus _bus;
        private readonly ILogger<AiAnalyticsService> _logger;

        public AiAnalyticsService(
            Context context,
            IAiRecommendationRepository recRepo,
            IPerformanceAlertRepository alertRepo,
            IAiPromptTemplateRepository templateRepo,
            IAnalyticsSnapshotRepository snapshotRepo,
            IAiProviderFactory providerFactory,
            IDistributedCache cache,
            IBus bus,
            ILogger<AiAnalyticsService> logger)
        {
            _context = context;
            _recRepo = recRepo;
            _alertRepo = alertRepo;
            _templateRepo = templateRepo;
            _snapshotRepo = snapshotRepo;
            _providerFactory = providerFactory;
            _cache = cache;
            _bus = bus;
            _logger = logger;
        }

        // ── Student: Learning Insights ──────────────────────────────────────
        public async Task<LearningInsightsDashboardDto> GetLearningInsightsAsync(
            int studentUserId, int academicYear, CancellationToken ct)
        {
            var student = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == studentUserId, ct)
                ?? throw new KeyNotFoundException("Student not found");

            // Get weakness analyses
            var weaknessRecs = await _context.AiRecommendations
                .Include(r => r.Subject)
                .Where(r => r.StudentUserId == studentUserId
                         && r.Type == RecommendationType.WeaknessAnalysis
                         && r.IsActive)
                .ToListAsync(ct);

            var analyses = weaknessRecs.Select(r => {
                var data = TryDeserialize<WeaknessAnalysisPayload>(r.Payload);
                return new WeaknessAnalysisDto(
                    r.SubjectId ?? 0,
                    r.Subject?.Name ?? "Unknown",
                    data?.WeakTopics?.Select(w => new WeakTopicDto(w.TopicName, w.ErrorRate, w.RecommendedActions ?? Enumerable.Empty<string>())) ?? Enumerable.Empty<WeakTopicDto>(),
                    data?.NarrativeSummary ?? "",
                    r.GeneratedAt);
            }).ToList();

            // Get content recommendations
            var contentRecs = await _context.AiRecommendations
                .Where(r => r.StudentUserId == studentUserId
                         && r.Type == RecommendationType.ContentRecommendation
                         && r.IsActive)
                .ToListAsync(ct);

            var recommendations = contentRecs.SelectMany(r => {
                var items = TryDeserialize<List<ContentRecPayload>>(r.Payload);
                return items?.Select(i => new ContentRecommendationDto(i.ContentItemId, i.Title, i.ContentType, i.Reason, i.RelevanceScore))
                    ?? Enumerable.Empty<ContentRecommendationDto>();
            }).ToList();

            // Get alerts
            var alerts = await GetAlertDtosForStudentAsync(studentUserId, ct);

            // Get performance trends
            var trends = await GetPerformanceTrendsAsync(student.StudentId, ct);

            return new LearningInsightsDashboardDto(analyses, recommendations, alerts, trends);
        }

        public async Task<IEnumerable<ContentRecommendationDto>> GetRecommendedContentAsync(
            int studentUserId, int subjectId, CancellationToken ct)
        {
            var rec = await _recRepo.GetActiveByStudentAndTypeAsync(
                studentUserId, subjectId, RecommendationType.ContentRecommendation, ct);

            if (rec == null) return Enumerable.Empty<ContentRecommendationDto>();

            var items = TryDeserialize<List<ContentRecPayload>>(rec.Payload);
            return items?.Select(i => new ContentRecommendationDto(
                i.ContentItemId, i.Title, i.ContentType, i.Reason, i.RelevanceScore))
                ?? Enumerable.Empty<ContentRecommendationDto>();
        }

        // ── Parent: Reports & Alerts ────────────────────────────────────────
        public async Task<ParentReportDto?> GetParentReportAsync(
            int parentUserId, int studentUserId, string reportMonth, CancellationToken ct)
        {
            await ValidateParentStudentLinkAsync(parentUserId, studentUserId, ct);

            var cacheKey = $"parent_report:{studentUserId}:{reportMonth}";
            var cached = await _cache.GetStringAsync(cacheKey, ct);
            if (cached != null) return JsonSerializer.Deserialize<ParentReportDto>(cached);

            var rec = await _context.AiRecommendations
                .Where(r => r.StudentUserId == studentUserId
                         && r.Type == RecommendationType.ParentReport
                         && r.IsActive
                         && r.Payload.Contains(reportMonth))
                .OrderByDescending(r => r.GeneratedAt)
                .FirstOrDefaultAsync(ct);

            if (rec == null) return null;
            return TryDeserialize<ParentReportDto>(rec.Payload);
        }

        public async Task<ParentReportDto> GenerateParentReportAsync(
            int parentUserId, int studentUserId, string reportMonth, CancellationToken ct)
        {
            await ValidateParentStudentLinkAsync(parentUserId, studentUserId, ct);

            // Check cache first (24h TTL)
            var cacheKey = $"parent_report:{studentUserId}:{reportMonth}";
            var cached = await _cache.GetStringAsync(cacheKey, ct);
            if (cached != null)
                return JsonSerializer.Deserialize<ParentReportDto>(cached)!;

            var student = await _context.Students
                .Include(s => s.User).Include(s => s.Grade)
                .FirstOrDefaultAsync(s => s.UserId == studentUserId, ct)
                ?? throw new KeyNotFoundException("Student not found");

            var performances = await _context.StudentPerformances
                .Include(p => p.Subject)
                .Where(p => p.StudentId == student.StudentId)
                .ToListAsync(ct);

            var attendanceRate = performances.Any()
                ? performances.Average(p => p.AttendanceRate) : 0m;

            var subjectSummaries = performances.Select(p => new SubjectSummaryDto(
                p.Subject?.Name ?? "Unknown", p.AvgExam, p.AttendanceRate,
                $"Average score: {p.AvgExam:F1}%")).ToList();

            var overallScore = performances.Any() ? performances.Average(p => p.AvgExam) : 0m;

            // Try AI narrative generation
            string narrative;
            try
            {
                narrative = await GenerateAiNarrativeAsync("parent_report",
                    new Dictionary<string, string>
                    {
                        { "student_name", student.User.FullName },
                        { "month", reportMonth },
                        { "performance_json", JsonSerializer.Serialize(subjectSummaries) },
                        { "attendance_json", JsonSerializer.Serialize(new { rate = attendanceRate }) },
                        { "language", "English" }
                    }, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AI narrative generation failed, using fallback");
                narrative = $"Performance report for {student.User.FullName} - {reportMonth}. " +
                    $"Overall score: {overallScore:F1}%, Attendance: {attendanceRate:F1}%.";
            }

            var report = new ParentReportDto(
                student.User.FullName, reportMonth, overallScore, attendanceRate,
                subjectSummaries, narrative,
                new[] { "Review weak subjects", "Maintain attendance", "Practice regularly" },
                DateTime.UtcNow);

            // Cache for 24 hours
            var reportJson = JsonSerializer.Serialize(report);
            await _cache.SetStringAsync(cacheKey, reportJson,
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24) }, ct);

            // Store as recommendation
            var rec = AiRecommendation.Create(studentUserId, null,
                RecommendationType.ParentReport, reportJson, "system", 0, 0, 24);
            await _recRepo.AddAsync(rec, ct);

            await _bus.Publish(new ParentReportReadyEvent(studentUserId, reportMonth), ct);
            return report;
        }

        public async Task<IEnumerable<PerformanceAlertDto>> GetStudentAlertsForParentAsync(
            int parentUserId, int studentUserId, CancellationToken ct)
        {
            await ValidateParentStudentLinkAsync(parentUserId, studentUserId, ct);
            return await GetAlertDtosForStudentAsync(studentUserId, ct);
        }

        // ── Teacher: Class Analytics & Suggestions ──────────────────────────
        public async Task<ClassAnalyticsDashboardDto> GetClassAnalyticsAsync(
            int teacherUserId, int classId, int subjectId, int academicYear, CancellationToken ct)
        {
            var cls = await _context.Classes.FindAsync(new object[] { classId }, ct)
                ?? throw new KeyNotFoundException("Class not found");
            var subject = await _context.Subjects.FindAsync(new object[] { subjectId }, ct)
                ?? throw new KeyNotFoundException("Subject not found");

            var year = academicYear.ToString();
            var performances = await _context.StudentPerformances
                .Include(p => p.Student).ThenInclude(s => s.User)
                .Where(p => p.SubjectId == subjectId && p.ClassId == classId && p.AcademicYear == year)
                .ToListAsync(ct);

            var classAvg = performances.Any() ? performances.Average(p => p.AvgExam) : 0m;

            var distribution = new[]
            {
                new ScoreDistributionBucketDto("0-20", performances.Count(p => p.AvgExam < 20)),
                new ScoreDistributionBucketDto("20-40", performances.Count(p => p.AvgExam >= 20 && p.AvgExam < 40)),
                new ScoreDistributionBucketDto("40-60", performances.Count(p => p.AvgExam >= 40 && p.AvgExam < 60)),
                new ScoreDistributionBucketDto("60-80", performances.Count(p => p.AvgExam >= 60 && p.AvgExam < 80)),
                new ScoreDistributionBucketDto("80-100", performances.Count(p => p.AvgExam >= 80))
            };

            var ordered = performances.OrderByDescending(p => p.AvgExam).ToList();
            var topFive = ordered.Take(5).Select(p => new AnalyticsStudentScoreDto(p.Student.UserId, p.Student.User.FullName, p.AvgExam));
            var bottomFive = ordered.TakeLast(5).Select(p => new AnalyticsStudentScoreDto(p.Student.UserId, p.Student.User.FullName, p.AvgExam));

            var sessions = await _context.Sessions
                .Where(s => s.ClassId == classId)
                .ToListAsync(ct);

            return new ClassAnalyticsDashboardDto(
                cls.Name, subject.Name, classAvg, distribution,
                topFive, bottomFive,
                sessions.Count,
                sessions.Count(s => s.Status == Domain.Enums.SessionStatus.Completed));
        }

        public async Task<TeachingSuggestionDto> GenerateTeachingSuggestionAsync(
            int teacherUserId, int studentUserId, int subjectId, CancellationToken ct)
        {
            var student = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == studentUserId, ct)
                ?? throw new KeyNotFoundException("Student not found");
            var subject = await _context.Subjects.FindAsync(new object[] { subjectId }, ct)
                ?? throw new KeyNotFoundException("Subject not found");

            string suggestion;
            try
            {
                suggestion = await GenerateAiNarrativeAsync("teaching_suggestion",
                    new Dictionary<string, string>
                    {
                        { "subject_name", subject.Name },
                        { "grade_name", "Grade" },
                        { "weak_topics_json", "[]" }
                    }, ct);
            }
            catch
            {
                suggestion = $"Consider providing additional practice for {student.User.FullName} in {subject.Name}.";
            }

            return new TeachingSuggestionDto(
                student.User.FullName, subject.Name, suggestion,
                new[] { "Review weak areas", "Provide practice sheets", "Schedule one-on-one session" },
                DateTime.UtcNow);
        }

        public async Task<StudentInsightDto> GetStudentInsightAsync(
            int teacherUserId, int studentUserId, int subjectId, CancellationToken ct)
        {
            var student = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == studentUserId, ct)
                ?? throw new KeyNotFoundException("Student not found");
            var subject = await _context.Subjects.FindAsync(new object[] { subjectId }, ct)
                ?? throw new KeyNotFoundException("Subject not found");

            // Get weakness analysis
            var weaknessRec = await _context.AiRecommendations
                .Where(r => r.StudentUserId == studentUserId
                         && r.SubjectId == subjectId
                         && r.Type == RecommendationType.WeaknessAnalysis
                         && r.IsActive)
                .OrderByDescending(r => r.GeneratedAt)
                .FirstOrDefaultAsync(ct);

            IEnumerable<WeakTopicDto> weakTopics = Enumerable.Empty<WeakTopicDto>();
            if (weaknessRec != null)
            {
                var data = TryDeserialize<WeaknessAnalysisPayload>(weaknessRec.Payload);
                weakTopics = data?.WeakTopics?.Select(w => new WeakTopicDto(w.TopicName, w.ErrorRate, w.RecommendedActions ?? Enumerable.Empty<string>())) ?? Enumerable.Empty<WeakTopicDto>();
            }

            // Get alerts
            var alerts = await _alertRepo.GetUnresolvedByStudentAsync(studentUserId, ct);
            var subjectAlerts = alerts.Where(a => a.SubjectId == subjectId)
                .Select(a => MapAlertDto(a, student.User.FullName, subject.Name)).ToList();

            // Get latest teaching suggestion
            var suggestionRec = await _context.AiRecommendations
                .Where(r => r.StudentUserId == studentUserId
                         && r.SubjectId == subjectId
                         && r.Type == RecommendationType.TeachingSuggestion
                         && r.IsActive)
                .OrderByDescending(r => r.GeneratedAt)
                .FirstOrDefaultAsync(ct);

            TeachingSuggestionDto? latestSuggestion = null;
            if (suggestionRec != null)
            {
                latestSuggestion = TryDeserialize<TeachingSuggestionDto>(suggestionRec.Payload);
            }

            return new StudentInsightDto(
                student.User.FullName,
                subject.Name,
                weakTopics,
                subjectAlerts,
                latestSuggestion);
        }

        // ── Admin: Platform Analytics ───────────────────────────────────────
        public async Task<PlatformAnalyticsDto> GetPlatformAnalyticsAsync(
            int academicYear, CancellationToken ct)
        {
            var totalStudents = await _context.Students.CountAsync(ct);
            var totalTeachers = await _context.Teachers.CountAsync(ct);
            var totalSubs = await _context.Subscriptions
                .CountAsync(s => s.Status == SubscriptionStatus.Active, ct);

            var thisMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var revenue = await _context.Payments
                .Where(p => p.CreatedAt >= thisMonth && p.Status == PaymentStatus.Completed)
                .SumAsync(p => p.Amount, ct);

            var totalSessions = await _context.Sessions.CountAsync(ct);
            var completedSessions = await _context.Sessions
                .CountAsync(s => s.Status == SessionStatus.Completed, ct);
            var sessionRate = totalSessions > 0 ? (decimal)completedSessions / totalSessions * 100 : 0m;

            var enrollments = await _context.Grades
                .Select(g => new GradeEnrollmentDto(g.Name, g.Students.Count))
                .ToListAsync(ct);

            return new PlatformAnalyticsDto(totalStudents, totalTeachers, totalSubs,
                revenue, sessionRate, enrollments);
        }

        public async Task<GradeHeatmapDto> GetGradeHeatmapAsync(
            int gradeId, int academicYear, CancellationToken ct)
        {
            var grade = await _context.Grades.FindAsync(new object[] { gradeId }, ct)
                ?? throw new KeyNotFoundException("Grade not found");

            var year = academicYear.ToString();
            var classes = await _context.Classes
                .Where(c => c.GradeId == gradeId)
                .ToListAsync(ct);

            var rows = new List<ClassHeatmapRowDto>();
            foreach (var cls in classes)
            {
                var perfs = await _context.StudentPerformances
                    .Include(p => p.Subject)
                    .Where(p => p.ClassId == cls.ClassId && p.AcademicYear == year)
                    .ToListAsync(ct);

                var subjectScores = perfs.GroupBy(p => p.Subject.Name)
                    .Select(g =>
                    {
                        var avg = g.Average(p => p.AvgExam);
                        var color = avg >= 80 ? "#22c55e" : avg >= 60 ? "#eab308" : "#ef4444";
                        return new SubjectScoreCellDto(g.Key, avg, color);
                    }).ToList();

                rows.Add(new ClassHeatmapRowDto(cls.Name, subjectScores));
            }

            return new GradeHeatmapDto(grade.Name, rows);
        }

        public async Task<IEnumerable<AiPromptTemplateDto>> GetAllPromptTemplatesAsync(CancellationToken ct)
        {
            var templates = await _templateRepo.GetAllAsync(ct);
            return templates.Select(t => new AiPromptTemplateDto(
                t.AiPromptTemplateId, t.Key, t.SystemPrompt, t.UserPromptTemplate,
                t.MaxTokens, t.Temperature, t.UpdatedAt, t.UpdatedBy));
        }

        public async Task UpdatePromptTemplateAsync(
            string key, UpdatePromptTemplateRequest request, string updatedBy, CancellationToken ct)
        {
            var template = await _templateRepo.GetByKeyAsync(key, ct)
                ?? throw new KeyNotFoundException($"Prompt template '{key}' not found");

            template.Update(request.SystemPrompt, request.UserPromptTemplate,
                request.MaxTokens, request.Temperature, updatedBy);
            await _templateRepo.UpdateAsync(template, ct);
        }

        // ── Rule-Based Alerts ───────────────────────────────────────────────
        public async Task<IEnumerable<PerformanceAlertDto>> EvaluatePerformanceAlertsAsync(
            int studentUserId, int subjectId, int classId, int academicYear, CancellationToken ct)
        {
            var student = await _context.Students
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.UserId == studentUserId, ct);
            if (student == null) return Enumerable.Empty<PerformanceAlertDto>();

            var year = academicYear.ToString();
            var alerts = new List<PerformanceAlertDto>();

            // Check attendance threshold (75%)
            var perf = await _context.StudentPerformances
                .FirstOrDefaultAsync(p => p.StudentId == student.StudentId
                    && p.SubjectId == subjectId && p.AcademicYear == year, ct);

            if (perf != null)
            {
                // Low attendance alert
                if (perf.AttendanceRate < 75m)
                {
                    var existing = await _alertRepo.GetUnresolvedAsync(studentUserId, subjectId, AlertType.LowAttendance, ct);
                    if (existing == null)
                    {
                        var alert = PerformanceAlert.Create(studentUserId, subjectId, AlertType.LowAttendance,
                            $"Attendance dropped to {perf.AttendanceRate:F1}% (threshold: 75%)",
                            perf.AttendanceRate, 75m);
                        await _alertRepo.AddAsync(alert, ct);
                        await _bus.Publish(new AlertCreatedEvent(studentUserId, subjectId, AlertType.LowAttendance, alert.Message), ct);
                        alerts.Add(MapAlertDto(alert, student.User.FullName, null));
                    }
                }

                // Low exam score alert (50%)
                if (perf.AvgExam < 50m && perf.TotalExamsTaken > 0)
                {
                    var existing = await _alertRepo.GetUnresolvedAsync(studentUserId, subjectId, AlertType.LowExamScore, ct);
                    if (existing == null)
                    {
                        var alert = PerformanceAlert.Create(studentUserId, subjectId, AlertType.LowExamScore,
                            $"Average exam score dropped to {perf.AvgExam:F1}% (threshold: 50%)",
                            perf.AvgExam, 50m);
                        await _alertRepo.AddAsync(alert, ct);
                        await _bus.Publish(new AlertCreatedEvent(studentUserId, subjectId, AlertType.LowExamScore, alert.Message), ct);
                        alerts.Add(MapAlertDto(alert, student.User.FullName, null));
                    }
                }
            }

            return alerts;
        }

        // ── AI Generation (triggered by consumers) ──────────────────────────
        public async Task GenerateWeaknessAnalysisAsync(
            int studentUserId, int subjectId, int classId, CancellationToken ct)
        {
            // Invalidate cache
            await _cache.RemoveAsync($"weakness:{studentUserId}:{subjectId}", ct);

            var subject = await _context.Subjects.FindAsync(new object[] { subjectId }, ct);
            if (subject == null) return;

            // Deactivate old
            await _recRepo.DeactivateAsync(studentUserId, subjectId, RecommendationType.WeaknessAnalysis, ct);

            // Create placeholder analysis (AI call would go here with real API keys)
            var payload = new WeaknessAnalysisPayload
            {
                WeakTopics = new List<WeakTopicPayload>
                {
                    new() { TopicName = "General Review", ErrorRate = 0.3m, RecommendedActions = new[] { "Review fundamentals", "Practice more" } }
                },
                NarrativeSummary = $"Analysis pending for {subject.Name}. Performance data has been updated."
            };

            var rec = AiRecommendation.Create(studentUserId, subjectId,
                RecommendationType.WeaknessAnalysis,
                JsonSerializer.Serialize(payload), "system", 0, 0, 24);
            await _recRepo.AddAsync(rec, ct);

            _logger.LogInformation("Weakness analysis generated for student {StudentUserId} subject {SubjectId}", studentUserId, subjectId);
        }

        // ── Private Helpers ─────────────────────────────────────────────────
        private async Task ValidateParentStudentLinkAsync(int parentUserId, int studentUserId, CancellationToken ct)
        {
            var linked = await _context.ParentStudentLinks
                .AnyAsync(l => l.ParentUserId == parentUserId && l.StudentUserId == studentUserId, ct);
            if (!linked) throw new UnauthorizedAccessException("Parent is not linked to this student");
        }

        private async Task<List<PerformanceAlertDto>> GetAlertDtosForStudentAsync(int studentUserId, CancellationToken ct)
        {
            var alerts = await _alertRepo.GetUnresolvedByStudentAsync(studentUserId, ct);
            var student = await _context.Users.FindAsync(new object[] { studentUserId }, ct);
            return alerts.Select(a => MapAlertDto(a, student?.FullName ?? "Unknown", a.Subject?.Name)).ToList();
        }

        private async Task<List<PerformanceTrendDto>> GetPerformanceTrendsAsync(int studentId, CancellationToken ct)
        {
            var exams = await _context.StudentExams
                .Include(se => se.Exam).ThenInclude(e => e.TeachingAssignment).ThenInclude(ta => ta.Subject)
                .Where(se => se.StudentId == studentId && se.Status == Domain.Enums.StudentExamStatus.Graded)
                .OrderBy(se => se.Exam.StartTime)
                .ToListAsync(ct);

            return exams.GroupBy(se => se.Exam.TeachingAssignment.Subject.Name)
                .Select(g => new PerformanceTrendDto(
                    g.Key,
                    g.Select(se => new ScorePointDto(se.Exam.StartTime, se.FinalScore ?? se.TotalScore ?? 0))))
                .ToList();
        }

        private async Task<string> GenerateAiNarrativeAsync(
            string templateKey, Dictionary<string, string> placeholders, CancellationToken ct)
        {
            var template = await _templateRepo.GetByKeyAsync(templateKey, ct);
            if (template == null)
                return "No template configured.";

            var userPrompt = template.UserPromptTemplate;
            foreach (var kv in placeholders)
                userPrompt = userPrompt.Replace($"{{{kv.Key}}}", SanitizeInput(kv.Value));

            var request = new AiPromptRequest(template.SystemPrompt, userPrompt, template.MaxTokens, template.Temperature);

            var provider = _providerFactory.GetProvider(AiProvider.OpenAI);
            try
            {
                var result = await provider.CompleteAsync(request, ct);
                return result.Content;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Primary AI provider failed, trying fallback");
                var fallback = _providerFactory.GetFallbackProvider(AiProvider.OpenAI);
                var result = await fallback.CompleteAsync(request, ct);
                return result.Content;
            }
        }

        private static string SanitizeInput(string input) =>
            input.Replace("{", "").Replace("}", "").Replace("\n", " ").Replace("\r", "");

        private static PerformanceAlertDto MapAlertDto(PerformanceAlert a, string studentName, string? subjectName) =>
            new(a.PerformanceAlertId, a.StudentUserId, studentName,
                a.SubjectId, subjectName, a.AlertType, a.Message,
                a.TriggerValue, a.Threshold, a.IsResolved, a.CreatedAt, a.ResolvedAt);

        private static T? TryDeserialize<T>(string json) where T : class
        {
            try { return JsonSerializer.Deserialize<T>(json); }
            catch { return null; }
        }

        // ── Internal payload types for JSON serialization ────────────────────
        private class WeaknessAnalysisPayload
        {
            public List<WeakTopicPayload>? WeakTopics { get; set; }
            public string? NarrativeSummary { get; set; }
        }

        private class WeakTopicPayload
        {
            public string TopicName { get; set; } = "";
            public decimal ErrorRate { get; set; }
            public IEnumerable<string>? RecommendedActions { get; set; }
        }

        private class ContentRecPayload
        {
            public int ContentItemId { get; set; }
            public string Title { get; set; } = "";
            public string ContentType { get; set; } = "";
            public string Reason { get; set; } = "";
            public decimal RelevanceScore { get; set; }
        }
    }
}
