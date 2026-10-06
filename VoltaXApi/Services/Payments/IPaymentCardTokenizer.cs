using VoltaXApi.Models;

namespace VoltaXApi.Services.Payments
{
    /// <summary>Display metadata the client claims for a card it tokenized with the payment provider.</summary>
    public sealed record CardDisplayMetadata(DebitCardTypeEnum Brand, string Last4, int ExpiryMonth, int ExpiryYear);

    /// <summary>A provider token that the payment provider has confirmed, with the card metadata it reports.</summary>
    public sealed record VerifiedCardToken(string Provider, string ProviderToken, CardDisplayMetadata Card);

    /// <summary>
    /// Verifies a card token issued by the payment provider. The API never receives a card number
    /// or CVV: the client hands card entry to the provider's hosted fields and sends back only the
    /// resulting token plus display metadata.
    /// </summary>
    public interface IPaymentCardTokenizer
    {
        /// <exception cref="VoltaXApi.Exceptions.ValidationException">The token is unknown, invalid or not accepted in this environment.</exception>
        Task<VerifiedCardToken> VerifyAsync(string providerToken, CardDisplayMetadata claimed, CancellationToken cancellationToken = default);
    }
}
