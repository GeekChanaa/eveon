using System.Security.Claims;

namespace VoltaXApi.Services.Audit
{
    // Who / where for an audit row, taken from the current request (null outside one).
    public sealed record AuditActor(int? UserID, string? Email, string? Role, string? IpAddress, string? CorrelationID)
    {
        public static AuditActor From(HttpContext? context)
        {
            if (context == null)
                return new AuditActor(null, null, "System", null, null);

            var principal = context.User;
            int? userId = int.TryParse(principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
            var correlation = context.Request.Headers["X-Correlation-ID"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(correlation))
                correlation = context.TraceIdentifier;

            return new AuditActor(
                userId,
                Truncate(principal?.FindFirstValue(ClaimTypes.Name), 256),
                Truncate(principal?.FindFirstValue(ClaimTypes.Role), 64),
                Truncate(context.Connection.RemoteIpAddress?.ToString(), 64),
                Truncate(correlation, 64));
        }

        internal static string? Truncate(string? value, int max)
            => value == null || value.Length <= max ? value : value.Substring(0, max);
    }

    public static class AuditSensitiveProperties
    {
        private static readonly string[] Fragments = { "Password", "Hash", "Token", "Secret", "Cvv", "CardNumber", "Salt", "BankAccount" };

        // "Pan" is matched case-sensitively so names like "Company" are not swallowed.
        public static bool IsSensitive(string propertyName) =>
            Fragments.Any(f => propertyName.Contains(f, StringComparison.OrdinalIgnoreCase)) ||
            propertyName.Contains("Pan", StringComparison.Ordinal) ||
            propertyName.Equals("PAN", StringComparison.OrdinalIgnoreCase);
    }
}
