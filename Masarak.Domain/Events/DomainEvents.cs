namespace Masarak.Domain.Events
{
    /// <summary>Raised when a subscription transitions to Active status.</summary>
    public record SubscriptionActivatedEvent(int UserId, int SubscriptionId, DateTime EndDate);

    /// <summary>Raised when a subscription transitions to Expired status.</summary>
    public record SubscriptionExpiredEvent(int UserId, int SubscriptionId);

    /// <summary>Raised when a parent is successfully linked to a student.</summary>
    public record ParentStudentLinkedEvent(int ParentUserId, int StudentUserId);
}
