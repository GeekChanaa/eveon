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
      private readonly IWalletService _walletService;

      public CardService(
        ICardRepository cardRepository,
        IConnectorRepository connectorRepository,
        IMailService mailService,
        ISystemReportService systemReportService,
        IWalletService walletService
      ) 
      {
        _cardRepository = cardRepository;
        _connectorRepository = connectorRepository;
        _mailService = mailService;
        _systemReportService = systemReportService;
        _walletService = walletService;
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

        public async Task<bool> AddAmountToCard(int cardID, decimal amount, int orderID)
        {
            if (amount <= 0)
                throw new ArgumentOutOfRangeException(nameof(amount), "A recharge amount must be positive");
            await _walletService.PostAsync(cardID, new WalletPosting(amount, WalletTransactionType.TopUp, $"order:{orderID}:topup",
                "Wallet top-up", OrderID: orderID));
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