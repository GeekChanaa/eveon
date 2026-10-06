using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.Services
{
    // Authorization of id tokens that are not EVEON cards (roaming tokens pushed by OCPI eMSPs).
    public interface IExternalTokenAuthorizer
    {
        // Null when the token is unknown to every external source; the caller then answers Invalid.
        Task<AuthorizationStatusEnumType?> AuthorizeAsync(string? idToken, string? chargePointId, CancellationToken cancellationToken = default);
    }

    // Lets roaming (OCPI) sessions follow OCPP transaction events without touching the card based flow.
    public interface IRoamingTransactionObserver
    {
        // Returns the status to answer when the transaction belongs to a roaming token (the local
        // card pipeline must then be skipped), null when it is a regular EVEON transaction.
        Task<AuthorizationStatusEnumType?> OnTransactionEventAsync(string chargePointId, TransactionEventRequest request, CancellationToken cancellationToken = default);
    }
}
