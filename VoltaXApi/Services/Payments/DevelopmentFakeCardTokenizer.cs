using System.Text.RegularExpressions;
using VoltaXApi.Exceptions;

namespace VoltaXApi.Services.Payments
{
    /// <summary>
    /// Stand-in for a real payment provider so card management can be exercised locally.
    /// Accepts only tokens shaped like "devtok_..." and only when the host runs in Development;
    /// in any other environment every call is rejected.
    /// </summary>
    public sealed class DevelopmentFakeCardTokenizer : IPaymentCardTokenizer
    {
        public const string ProviderName = "development-fake";
        private static readonly Regex TokenShape = new("^devtok_[A-Za-z0-9]{16,64}$", RegexOptions.Compiled);

        private readonly IHostEnvironment _environment;

        public DevelopmentFakeCardTokenizer(IHostEnvironment environment)
        {
            _environment = environment;
        }

        public Task<VerifiedCardToken> VerifyAsync(string providerToken, CardDisplayMetadata claimed, CancellationToken cancellationToken = default)
        {
            if (!_environment.IsDevelopment())
                throw new ValidationException("Card entry via payment provider is not available yet.");
            if (string.IsNullOrEmpty(providerToken) || !TokenShape.IsMatch(providerToken))
                throw new ValidationException("Invalid card token.");

            return Task.FromResult(new VerifiedCardToken(ProviderName, providerToken, claimed));
        }
    }
}
