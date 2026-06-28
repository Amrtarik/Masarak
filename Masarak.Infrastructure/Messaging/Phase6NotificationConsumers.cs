using Masarak.Application.Interfaces;
using Masarak.Domain.Constants;
using Masarak.Domain.Enums;
using Masarak.Domain.Events;
using Masarak.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Masarak.Infrastructure.Messaging
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Phase 6: RabbitMQ Notification Consumers
    //
    // Each consumer follows the pattern:
    //   1. Handle event
    //   2. Determine target user(s)
    //   3. Call INotificationService.CreateAndPushAsync
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Notifies student when subscription is activated.
    /// Trigger: SubscriptionActivatedEvent (Phase 1)
    /// </summary>
    public class SubscriptionActivatedNotificationConsumer : IConsumer<SubscriptionActivatedEvent>
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<SubscriptionActivatedNotificationConsumer> _logger;

        public SubscriptionActivatedNotificationConsumer(
            INotificationService notificationService,
            ILogger<SubscriptionActivatedNotificationConsumer> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<SubscriptionActivatedEvent> context)
        {
            var msg = context.Message;
            _logger.LogInformation("Phase 6: SubscriptionActivated for User {UserId}", msg.UserId);

            await _notificationService.CreateAndPushAsync(
                msg.UserId,
                NotificationType.SubscriptionActivated,
                "Subscription Activated",
                $"Your subscription is now active! It expires on {msg.EndDate:yyyy-MM-dd}.",
                "/student/subscription",
                context.CancellationToken);
        }
    }

    /// <summary>
    /// Notifies student 7 days before subscription expires.
    /// Trigger: SubscriptionExpiringEvent (Phase 1)
    /// </summary>
    public class SubscriptionExpiringNotificationConsumer : IConsumer<SubscriptionExpiringEvent>
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<SubscriptionExpiringNotificationConsumer> _logger;

        public SubscriptionExpiringNotificationConsumer(
            INotificationService notificationService,
            ILogger<SubscriptionExpiringNotificationConsumer> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<SubscriptionExpiringEvent> context)
        {
            var msg = context.Message;
            _logger.LogInformation("Phase 6: SubscriptionExpiring for User {UserId}, {Days} days remaining",
                msg.UserId, msg.DaysRemaining);

            await _notificationService.CreateAndPushAsync(
                msg.UserId,
                NotificationType.SubscriptionExpiring,
                "Subscription Expiring Soon",
                $"Your subscription expires in {msg.DaysRemaining} days. Renew now to avoid interruption.",
                "/student/subscription",
                context.CancellationToken);
        }
    }

    /// <summary>
    /// Notifies student and linked parent(s) when an exam is fully graded.
    /// Trigger: ExamFullyGradedEvent (Phase 3)
    /// </summary>
    public class ExamFullyGradedNotificationConsumer : IConsumer<ExamFullyGradedEvent>
    {
        private readonly INotificationService _notificationService;
        private readonly Context _context;
        private readonly ILogger<ExamFullyGradedNotificationConsumer> _logger;

        public ExamFullyGradedNotificationConsumer(
            INotificationService notificationService,
            Context context,
            ILogger<ExamFullyGradedNotificationConsumer> logger)
        {
            _notificationService = notificationService;
            _context = context;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<ExamFullyGradedEvent> context)
        {
            var msg = context.Message;
            _logger.LogInformation("Phase 6: ExamFullyGraded for Student {StudentUserId}, StudentExam {StudentExamId}",
                msg.StudentUserId, msg.StudentExamId);

            // Notify the student
            await _notificationService.CreateAndPushAsync(
                msg.StudentUserId,
                NotificationType.ExamGraded,
                "Exam Graded",
                "Your exam has been graded. Check your results now.",
                $"/student/exams/{msg.StudentExamId}",
                context.CancellationToken);

            // Notify linked parents
            var parentUserIds = await _context.ParentStudentLinks
                .Where(l => l.StudentUserId == msg.StudentUserId)
                .Select(l => l.ParentUserId)
                .ToListAsync(context.CancellationToken);

            foreach (var parentUserId in parentUserIds)
            {
                await _notificationService.CreateAndPushAsync(
                    parentUserId,
                    NotificationType.StudentExamResult,
                    "Student Exam Result",
                    "Your child's exam has been graded. View the results.",
                    $"/parent/reports/{msg.StudentUserId}",
                    context.CancellationToken);
            }
        }
    }

    /// <summary>
    /// Notifies student when an assignment is graded.
    /// Trigger: AssignmentGradedEvent (Phase 3)
    /// </summary>
    public class AssignmentGradedNotificationConsumer : IConsumer<AssignmentGradedEvent>
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<AssignmentGradedNotificationConsumer> _logger;

        public AssignmentGradedNotificationConsumer(
            INotificationService notificationService,
            ILogger<AssignmentGradedNotificationConsumer> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<AssignmentGradedEvent> context)
        {
            var msg = context.Message;
            _logger.LogInformation("Phase 6: AssignmentGraded for Student {StudentUserId}, Submission {SubmissionId}",
                msg.StudentUserId, msg.SubmissionId);

            await _notificationService.CreateAndPushAsync(
                msg.StudentUserId,
                NotificationType.ExamGraded, // reuses ExamGraded type — could be a separate "AssignmentGraded" type
                "Assignment Graded",
                "Your assignment submission has been graded. Check your feedback.",
                $"/student/assignments",
                context.CancellationToken);
        }
    }

    /// <summary>
    /// Notifies all enrolled students when a session is scheduled.
    /// Trigger: SessionScheduledEvent (Phase 2)
    /// </summary>
    public class SessionScheduledNotificationConsumer : IConsumer<SessionScheduledEvent>
    {
        private readonly INotificationService _notificationService;
        private readonly Context _context;
        private readonly ILogger<SessionScheduledNotificationConsumer> _logger;

        public SessionScheduledNotificationConsumer(
            INotificationService notificationService,
            Context context,
            ILogger<SessionScheduledNotificationConsumer> logger)
        {
            _notificationService = notificationService;
            _context = context;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<SessionScheduledEvent> context)
        {
            var msg = context.Message;
            _logger.LogInformation("Phase 6: SessionScheduled for Class {ClassId}, Session {SessionId}",
                msg.ClassId, msg.SessionId);

            // Get all enrolled student user IDs for the class
            var studentUserIds = await _context.StudentClasses
                .Include(sc => sc.Student)
                .Where(sc => sc.ClassId == msg.ClassId && sc.IsActive)
                .Select(sc => sc.Student.UserId)
                .ToListAsync(context.CancellationToken);

            foreach (var studentUserId in studentUserIds)
            {
                await _notificationService.CreateAndPushAsync(
                    studentUserId,
                    NotificationType.NewContent,
                    "New Session Scheduled",
                    $"A new session \"{msg.Title}\" has been scheduled for {msg.ScheduledAt:yyyy-MM-dd HH:mm}.",
                    $"/student/schedule",
                    context.CancellationToken);
            }
        }
    }

    /// <summary>
    /// Notifies student and linked parent(s) about performance alerts.
    /// Trigger: AlertCreatedEvent (Phase 5)
    /// </summary>
    public class AlertCreatedNotificationConsumer : IConsumer<AlertCreatedEvent>
    {
        private readonly INotificationService _notificationService;
        private readonly Context _context;
        private readonly ILogger<AlertCreatedNotificationConsumer> _logger;

        public AlertCreatedNotificationConsumer(
            INotificationService notificationService,
            Context context,
            ILogger<AlertCreatedNotificationConsumer> logger)
        {
            _notificationService = notificationService;
            _context = context;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<AlertCreatedEvent> context)
        {
            var msg = context.Message;
            _logger.LogInformation("Phase 6: AlertCreated for Student {StudentUserId}, Type {AlertType}",
                msg.StudentUserId, msg.AlertType);

            // Determine notification type based on alert type
            var notifType = msg.AlertType switch
            {
                AlertType.LowAttendance => NotificationType.AttendanceAlert,
                AlertType.LowExamScore  => NotificationType.LowPerformanceAlert,
                _                       => NotificationType.LowPerformanceAlert
            };

            // Notify the student
            await _notificationService.CreateAndPushAsync(
                msg.StudentUserId,
                notifType,
                "Performance Alert",
                msg.Message,
                "/student/insights",
                context.CancellationToken);

            // Notify linked parents
            var parentUserIds = await _context.ParentStudentLinks
                .Where(l => l.StudentUserId == msg.StudentUserId)
                .Select(l => l.ParentUserId)
                .ToListAsync(context.CancellationToken);

            var parentNotifType = msg.AlertType == AlertType.LowAttendance
                ? NotificationType.StudentAttendanceAlert
                : NotificationType.LowPerformanceAlert;

            foreach (var parentUserId in parentUserIds)
            {
                await _notificationService.CreateAndPushAsync(
                    parentUserId,
                    parentNotifType,
                    "Student Alert",
                    msg.Message,
                    $"/parent/reports/{msg.StudentUserId}",
                    context.CancellationToken);
            }
        }
    }

    /// <summary>
    /// Notifies parent when a monthly smart report is ready.
    /// Trigger: ParentReportReadyEvent (Phase 5)
    /// </summary>
    public class ParentReportReadyNotificationConsumer : IConsumer<ParentReportReadyEvent>
    {
        private readonly INotificationService _notificationService;
        private readonly Context _context;
        private readonly ILogger<ParentReportReadyNotificationConsumer> _logger;

        public ParentReportReadyNotificationConsumer(
            INotificationService notificationService,
            Context context,
            ILogger<ParentReportReadyNotificationConsumer> logger)
        {
            _notificationService = notificationService;
            _context = context;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<ParentReportReadyEvent> context)
        {
            var msg = context.Message;
            _logger.LogInformation("Phase 6: ParentReportReady for Student {StudentUserId}, Month {ReportMonth}",
                msg.StudentUserId, msg.ReportMonth);

            // Notify all linked parents
            var parentUserIds = await _context.ParentStudentLinks
                .Where(l => l.StudentUserId == msg.StudentUserId)
                .Select(l => l.ParentUserId)
                .ToListAsync(context.CancellationToken);

            foreach (var parentUserId in parentUserIds)
            {
                await _notificationService.CreateAndPushAsync(
                    parentUserId,
                    NotificationType.MonthlyReportReady,
                    "Monthly Report Ready",
                    $"Your monthly report for {msg.ReportMonth} is ready to view.",
                    $"/parent/reports/{msg.StudentUserId}/{msg.ReportMonth}",
                    context.CancellationToken);
            }
        }
    }

    /// <summary>
    /// Notifies all admin users when a payment fails.
    /// Trigger: PaymentFailedEvent (Phase 1)
    /// </summary>
    public class PaymentFailedNotificationConsumer : IConsumer<PaymentFailedEvent>
    {
        private readonly INotificationService _notificationService;
        private readonly ILogger<PaymentFailedNotificationConsumer> _logger;

        public PaymentFailedNotificationConsumer(
            INotificationService notificationService,
            ILogger<PaymentFailedNotificationConsumer> logger)
        {
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<PaymentFailedEvent> context)
        {
            var msg = context.Message;
            _logger.LogInformation("Phase 6: PaymentFailed for User {UserId}, Subscription {SubscriptionId}: {Reason}",
                msg.UserId, msg.SubscriptionId, msg.Reason);

            // Notify all admins
            await _notificationService.CreateAndPushToRoleAsync(
                AppRoles.Admin,
                NotificationType.PaymentFailed,
                "Payment Failed",
                $"Payment failed for User {msg.UserId} (Subscription {msg.SubscriptionId}): {msg.Reason}",
                "/admin/subscriptions",
                context.CancellationToken);
        }
    }
}
