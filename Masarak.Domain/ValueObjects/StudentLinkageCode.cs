namespace Masarak.Domain.ValueObjects
{
    /// <summary>
    /// An 8-character alphanumeric code generated for Student users.
    /// Used by parents to link their account to a student.
    /// </summary>
    public record StudentLinkageCode(string Value)
    {
        /// <summary>
        /// Generates a random 8-character uppercase alphanumeric code.
        /// </summary>
        public static StudentLinkageCode Generate() =>
            new(Guid.NewGuid().ToString("N")[..8].ToUpper());
    }
}
