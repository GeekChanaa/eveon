using VoltaXApi.Models;
using VoltaXApi.Dtos;
using VoltaXApi.Data;
using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.Services
{
    public class CardService : ICardService
    {
      private readonly ICardRepository _cardRepository;
      private readonly IConnectorRepository _connectorRepository;
      private readonly IMailService _mailService;
      private readonly ISystemReportService _systemReportService;

      public CardService(
        ICardRepository cardRepository,
        IConnectorRepository connectorRepository,
        IMailService mailService,
        ISystemReportService systemReportService
      ) 
      {
        _cardRepository = cardRepository;
        _connectorRepository = connectorRepository;
        _mailService = mailService;
        _systemReportService = systemReportService;
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

        public async Task<bool> SubstractAmountFromCard(int cardTagID, double kwhCharged, int connectorID)
        {
            Card? card = await _cardRepository.GetByIdAsync(cardTagID);
            Connector? connector = await _connectorRepository.GetByIdAsync(connectorID);

            double price = kwhCharged * (double)connector.PricePerKWh;
            card.Balance = card.Balance - price;
            await _cardRepository.Update(card);

            if(card.Balance < 0){
                await HandleCardNegativeBalance(card, connectorID);
                return false;
            }
            return true;
        }
        
        public async Task<bool> AddAmountToCard(int cardID, double amount)
        {
            Card? card = await _cardRepository.GetByIdAsync(cardID);

            card.Balance = card.Balance + amount;
            await _cardRepository.Update(card);

            return true;
        }

        public async Task HandleCardNegativeBalance(Card card, int connectorID)
        {
            SystemReport report = new()
            {
                IsEmail = true,
                IsNotification = true,
                CardID = card.ID,
                ConnectorID = connectorID,
                IssueDescription = "Negative balance of " + card.Balance + " for the card : #" + card.ID,
                Status = ReportStatusEnum.Pending,
                Criticality = ReportCriticality.High
            };
            await _systemReportService.HandleReport(report);
        }


    }

    
}