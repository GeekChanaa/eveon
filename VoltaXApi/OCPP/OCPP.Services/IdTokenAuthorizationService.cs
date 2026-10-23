using VoltaXApi.Data;
using VoltaXApi.Models;
using VoltaXApi.OCPP.Helpers;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.Services;

namespace VoltaXApi.OCPP.Services
{
    /// <summary>
    /// Authorize of an id token / idTag for any OCPP version: refused while the charge point is not Accepted,
    /// EVEON cards are checked locally (blocked, expired, balance below the minimum to start: NoCredit), other tokens
    /// go to the external (OCPI) authorizer.
    /// </summary>
    public class IdTokenAuthorizationService
    {
        private readonly ICardRepository _cardRepository;
        private readonly IExternalTokenAuthorizer _externalTokenAuthorizer;
        private readonly VoltaXApiDbContext _db;
        private readonly ILogger<IdTokenAuthorizationService> _logger;
        private readonly GlobalConfigurations _pricing;

        public IdTokenAuthorizationService(ICardRepository cardRepository, IExternalTokenAuthorizer externalTokenAuthorizer,
            VoltaXApiDbContext db, ILogger<IdTokenAuthorizationService> logger, GlobalConfigurations pricing)
        {
            _cardRepository = cardRepository;
            _externalTokenAuthorizer = externalTokenAuthorizer;
            _db = db;
            _logger = logger;
            _pricing = pricing;
        }

        /// <summary>A card may start a session: positive balance, at least DefaultMinimumRequiredBalance.</summary>
        public static bool HasCredit(Card card, GlobalConfigurations pricing) =>
            card.Balance > 0 && card.Balance >= pricing.DefaultMinimumRequiredBalance;

        /// <summary>Never throws: a token that cannot be verified is Invalid.</summary>
        public async Task<AuthorizationStatusEnumType> AuthorizeAsync(string? idTag, string chargePointId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!await ChargePointRegistration.IsAcceptedAsync(_db, chargePointId))
                {
                    // A charge point that is not Accepted (Pending provisioning or unknown) may not start charging.
                    _logger.LogWarning("Authorize => Charge point {ChargePointId} is not accepted; token refused", chargePointId);
                    return AuthorizationStatusEnumType.Invalid;
                }
                return await GetTokenStatus(idTag, chargePointId, cancellationToken);
            }
            catch (Exception exp)
            {
                _logger.LogError(exp, "Authorize => Could not verify token {IdTag} for {ChargePointId}", idTag, chargePointId);
                return AuthorizationStatusEnumType.Invalid;
            }
        }

        private async Task<AuthorizationStatusEnumType> GetTokenStatus(string? idTag, string chargePointId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(idTag))
                return AuthorizationStatusEnumType.Invalid;

            Card? card = await _cardRepository.GetCardByNumber(idTag);
            if (card == null)
                return await _externalTokenAuthorizer.AuthorizeAsync(idTag, chargePointId, cancellationToken) ?? AuthorizationStatusEnumType.Invalid;
            if (card.Blocked == true)
                return AuthorizationStatusEnumType.Blocked;
            if (card.ExpirationDate < DateTime.UtcNow)
                return AuthorizationStatusEnumType.Expired;
            if (!HasCredit(card, _pricing))
                return AuthorizationStatusEnumType.NoCredit;
            return AuthorizationStatusEnumType.Accepted;
        }
    }
}
