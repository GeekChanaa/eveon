using VoltaXApi.Exceptions;
using VoltaXApi.Models;
using VoltaXApi.Data;
using VoltaXApi.Dtos;
using Microsoft.AspNetCore.Http;

namespace VoltaXApi.Services
{
    public class OrderService : IOrderService
    {
        private readonly ICardRepository _cardRepository;
        private readonly ICardService _cardService;
        private readonly IDebitCardRepository _debitCardRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IMailService _mailService;
        private readonly IInvoiceGeneratorService<InvoiceData> _invoiceService;
        private readonly IWalletService _walletService;
        private readonly IUserRepository _userRepository;

        public OrderService(
            ICardRepository cardRepository,
            IDebitCardRepository debitCardRepository,
            IOrderRepository orderRepository,
            ICardService cardService,
            IMailService mailService,
            IInvoiceGeneratorService<InvoiceData> invoiceService,
            IWalletService walletService,
            IUserRepository userRepository)
        {
            _walletService = walletService;
            _userRepository = userRepository;
            _cardRepository = cardRepository;
            _debitCardRepository = debitCardRepository;
            _orderRepository = orderRepository;
            _cardService = cardService;
            _mailService = mailService;
            _invoiceService = invoiceService;
        }

        public async Task<Order> CreateManualTopUp(CreateRechargeOrderDto orderDto, int operatorUserId)
        {
            var card = await _cardRepository.GetByIdAsync(orderDto.CardID)
                ?? throw new CardNotFoundException($"There is no card with the id {orderDto.CardID}");
            var order = await _orderRepository.CreateRechargeOrder(orderDto);
            order.Status = RechargeOrderStatus.Completed;
            order.Provider = "Manual";
            order.PaidAt = DateTime.UtcNow;
            order.CreatedByUserID = operatorUserId;
            await _orderRepository.Update(order);
            await _walletService.PostAsync(order.CardID, new WalletPosting(order.Amount, WalletTransactionType.TopUp, $"order:{order.ID}:topup",
                $"Manual top-up: {orderDto.Note}", OrderID: order.ID, CreatedByUserID: operatorUserId));

            var owner = card.UserID == null ? null : await _userRepository.GetByIdAsync(card.UserID.Value);
            if (owner != null)
                await SendQuoteAsync(order, owner.Email, owner.FullName);
            return order;
        }

        private async Task SendQuoteAsync(Order order, string email, string userName)
        {
            var invoice = await _orderRepository.GetOrderForInvoice(order.ID);
            if (invoice == null)
                return;
            {
                var pdf = _invoiceService.GenerateInvoice(invoice);
                await using var stream = new MemoryStream(pdf);
                var attachment = new FormFile(stream, 0, pdf.Length, "quote", $"Recharge-quote-{order.ID}.pdf")
                {
                    Headers = new HeaderDictionary(),
                    ContentType = "application/pdf"
                };
                await _mailService.SendEmailAsync(new MailRequest
                {
                    Email = email,
                    Name = userName,
                    ToEmails = new List<string> { email },
                    Subject = $"Recharge quote #{order.ID}",
                    Body = $"Your recharge payment is {order.Status}. The quote for {order.Amount:0.00} is attached.",
                    Attachments = new List<IFormFile> { attachment }
                });
            }
        }
    }
}
