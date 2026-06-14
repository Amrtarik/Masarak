namespace Masarak.Domain.ValueObjects
{
    /// <summary>
    /// Represents a monetary amount with its currency.
    /// </summary>
    public record Money(decimal Amount, string Currency)
    {
        public static Money Zero(string currency) => new(0, currency);
    }
}
