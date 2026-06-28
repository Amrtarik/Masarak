using Masarak.Application.Interfaces;
using Masarak.Domain.Events;
using MassTransit;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Masarak.Infrastructure.Messaging
{
    /// <summary>
    /// Phase 5: Consumes PerformanceRecalculatedEvent to trigger AI analysis and alert evaluation.
    /// Steps:
    ///   1. Invalidates Redis cache for affected student/subject
    ///   2. Triggers weakness analysis generation
    ///   3. Evaluates rule-based performance alerts
    /// </summary>
    public class PerformanceRecalculatedAiConsumer : IConsumer<PerformanceRecalculatedEvent>
    {
        private readonly IAiAnalyticsService _aiService;
        private readonly IDistributedCache _cache;
        private readonly ILogger<PerformanceRecalculatedAiConsumer> _logger;

        public PerformanceRecalculatedAiConsumer(
            IAiAnalyticsService aiService,
            IDistributedCache cache,
            ILogger<PerformanceRecalculatedAiConsumer> logger)
        {
            _aiService = aiService;
            _cache = cache;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<PerformanceRecalculatedEvent> context)
        {
            var msg = context.Message;
            _logger.LogInformation(
                "Phase 5: Processing PerformanceRecalculatedEvent for Student {StudentUserId}, Subject {SubjectId}, Class {ClassId}",
                msg.StudentUserId, msg.SubjectId, msg.ClassId);

            // 1. Invalidate Redis cache
            await _cache.RemoveAsync($"weakness:{msg.StudentUserId}:{msg.SubjectId}", context.CancellationToken);
            await _cache.RemoveAsync($"recommendations:{msg.StudentUserId}:{msg.SubjectId}", context.CancellationToken);

            // 2. Generate weakness analysis
            try
            {
                await _aiService.GenerateWeaknessAnalysisAsync(
                    msg.StudentUserId, msg.SubjectId, msg.ClassId, context.CancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate weakness analysis for Student {StudentUserId}", msg.StudentUserId);
            }

            // 3. Evaluate rule-based alerts
            try
            {
                await _aiService.EvaluatePerformanceAlertsAsync(
                    msg.StudentUserId, msg.SubjectId, msg.ClassId, 2026, context.CancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to evaluate alerts for Student {StudentUserId}", msg.StudentUserId);
            }
        }
    }
}
