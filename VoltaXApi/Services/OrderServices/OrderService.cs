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

        public OrderService(
            ICardRepository cardRepository,
            IDebitCardRepository debitCardRepository,
            IOrderRepository orderRepository,
            ICardService cardService,
            IMailService mailService,
            IInvoiceGeneratorService<InvoiceData> invoiceService)
        {
            _cardRepository = cardRepository;
            _debitCardRepository = debitCardRepository;
            _orderRepository = orderRepository;
            _cardService = cardService;
            _mailService = mailService;
            _invoiceService = invoiceService;
        }

        public async Task CreateOrder(CreateRechargeOrderDto orderDto)
        {
            var order = await this._orderRepository.CreateRechargeOrder(orderDto);
            if (order.Status == RechargeOrderStatus.Completed)
                await this._cardService.AddAmountToCard(orderDto.CardID, orderDto.Amount);
            var card = (await _cardRepository.GetAllCards()).FirstOrDefault(card => card.ID == order.CardID);
            if (card?.User != null)
                await SendQuoteAsync(order, card.User.Email, card.User.FullName);
        }

        public async Task<MockPaymentResultDto> ProcessPayment(RechargeOrderDto orderDto)
        {
            if (orderDto.RechargeAmount <= 0)
                throw new ArgumentOutOfRangeException(nameof(orderDto.RechargeAmount), "Recharge amount must be greater than zero.");

            var card = (await _cardRepository.GetAllCards()).FirstOrDefault(card => card.ID == orderDto.CardID);
            if (card == null || card.User == null)
                throw new InvalidOperationException("The selected recharge card could not be found.");
            if (card.UserID != orderDto.UserID)
                throw new UnauthorizedAccessException("A recharge can only be made for one of the user's own cards.");
            EmailVerificationGuard.EnsureVerified(card.User);

            var status = orderDto.MockPaymentStatus ?? RechargeOrderStatus.Completed;
            var order = await _orderRepository.CreateRechargeOrder(new CreateRechargeOrderDto
            {
                CardID = orderDto.CardID,
                Amount = orderDto.RechargeAmount,
                Status = status,
                RechargeDate = DateTime.UtcNow
            });

            var credited = status == RechargeOrderStatus.Completed;
            if (credited)
                await _cardService.AddAmountToCard(order.CardID, order.Amount);

            await SendQuoteAsync(order, card.User.Email, card.User.FullName);

            return new MockPaymentResultDto
            {
                OrderID = order.ID,
                CardID = order.CardID,
                Amount = order.Amount,
                Status = status,
                BalanceCredited = credited,
                Message = credited ? "Mock payment completed and the recharge card was credited." : $"Mock payment recorded with status {status}. The recharge card was not credited."
            };
        }

        private async Task SendQuoteAsync(Order order, string email, string userName)
        {
            var invoice = await _orderRepository.GetOrderForInvoice(order.ID);
            var path = Path.Combine(Path.GetTempPath(), $"voltax-recharge-{order.ID}-{Guid.NewGuid():N}.pdf");
            try
            {
                var pdf = _invoiceService.GenerateInvoice(invoice, path);
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
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }
    }
}
