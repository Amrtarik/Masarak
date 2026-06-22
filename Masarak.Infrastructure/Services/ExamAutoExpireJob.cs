using Masarak.Application.Interfaces;
using Masarak.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Masarak.Infrastructure.Services
{
    /// <summary>
    /// Background service that periodically sweeps for StudentExam attempts 
    /// where ExpiresAt has passed but Status is still InProgress.
    /// It auto-submits them and triggers grading.
    /// </summary>
    public class ExamAutoExpireJob : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ExamAutoExpireJob> _logger;

        public ExamAutoExpireJob(IServiceProvider serviceProvider, ILogger<ExamAutoExpireJob> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ExamAutoExpireJob is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessExpiredExamsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing ExamAutoExpireJob.");
                }

                // Wait 1 minute before next sweep
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }

            _logger.LogInformation("ExamAutoExpireJob is stopping.");
        }

        private async Task ProcessExpiredExamsAsync(CancellationToken ct)
        {
            using var scope = _serviceProvider.CreateScope();
            var studentExamRepo = scope.ServiceProvider.GetRequiredService<IStudentExamRepository>();
            // IAssessmentService assessmentService = scope.ServiceProvider.GetRequiredService<IAssessmentService>();
            // Instead of circular dependency, we do the logic here or in a dedicated usecase.
            // For now, let's mark them as AutoExpired/Submitted.

            var now = DateTime.UtcNow;
            var expiredExams = await studentExamRepo.GetExpiredInProgressAsync(now, ct);

            if (!expiredExams.Any()) return;

            _logger.LogInformation($"Found {expiredExams.Count()} expired exams to auto-submit.");

            foreach (var attempt in expiredExams)
            {
                attempt.Status = StudentExamStatus.AutoExpired;
                attempt.SubmittedAt = attempt.ExpiresAt; // Mark submission time as the exact expiry time

                // Run auto-grading logic over it
                decimal totalAutoScore = 0;
                bool needsManualGrading = false;
                var autoGradingService = new Masarak.Domain.Services.AutoGradingService();

                foreach (var answer in attempt.StudentAnswers)
                {
                    if (answer.Question.IsAutoGraded)
                    {
                        var score = autoGradingService.Grade(answer.Question, answer);
                        totalAutoScore += score;
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
                }
                else
                {
                    attempt.HasPendingManualGrading = true;
                }

                await studentExamRepo.UpdateAsync(attempt, ct);

                // Future: Dispatch ExamSubmittedEvent here
            }
        }
    }
}
