using VoltaXApi.Models;
using VoltaXApi.Dtos;
using VoltaXApi.Data;
using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.Services
{
    public class CardService : ICardService
    {
      private readonly ICardRepository _cardRepository;

      public CardService(
        ICardRepository cardRepository
      ) 
      {
        _cardRepository = cardRepository;
      }

      public async Task<AuthorizationStatusEnumType> ValidateCard(string idTag)
        {
            if (string.IsNullOrWhiteSpace(idTag))
            {
                return AuthorizationStatusEnumType.Accepted;
            }
            var card = (await _cardRepository.FindAsync(c => c.CardNumber == idTag)).FirstOrDefault();

            if (card == null)
                return AuthorizationStatusEnumType.Unknown;

            if (card.Blocked.HasValue && card.Blocked.Value)
                return AuthorizationStatusEnumType.Blocked;

            if (card.ExpirationDate < DateTime.Now)
                return AuthorizationStatusEnumType.Expired;

            return AuthorizationStatusEnumType.Accepted;
        }
    }
}