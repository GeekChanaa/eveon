using VoltaXApi.Models;
using VoltaXApi.Dtos;
using VoltaXApi.Data;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.Exceptions;

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
            var card = await _cardRepository.GetCardByNumber(idTag);

            if (card == null)
                return AuthorizationStatusEnumType.Unknown;

            if (card.Blocked.HasValue && card.Blocked.Value)
                return AuthorizationStatusEnumType.Blocked;

            if (card.ExpirationDate < DateTime.UtcNow)
                return AuthorizationStatusEnumType.Expired;

            return AuthorizationStatusEnumType.Accepted;
        }

        public Task<bool> SubstractAmountFromCard(int cardTagID, double kwhCharged, int connectorID) =>
            Debit(cardTagID, connectorID, connector => CostCalculator.Instance.EnergyCost(connector, kwhCharged));

        public Task<bool> SubstractAmountFromCardByMinutes(int cardTagID, double minutesCharged, int connectorID) =>
            Debit(cardTagID, connectorID, connector => CostCalculator.Instance.FinalCost(connector, minutesCharged));

        public Task<bool> SubstractAmountFromCardByIdleMinutes(int cardTagID, double idleMinutes, int connectorID) =>
            Debit(cardTagID, connectorID, connector => CostCalculator.Instance.IdleCost(connector, idleMinutes));

        /// <summary>Debits the card with the connector-priced amount; false (and a system report) when the balance goes negative.</summary>
        private async Task<bool> Debit(int cardID, int connectorID, Func<Connector, double> price)
        {
            Card card = await _cardRepository.GetByIdAsync(cardID)
                ?? throw new CardNotFoundException($"There is no card with the id {cardID}");
            Connector connector = await _connectorRepository.GetByIdAsync(connectorID)
                ?? throw new NotFoundException($"There is no connector with the id {connectorID}");

            card.Balance -= price(connector);
            await _cardRepository.Update(card);

            if (card.Balance < 0)
            {
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