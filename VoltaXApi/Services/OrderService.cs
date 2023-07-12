using VoltaXApi.Models;
using VoltaXApi.Data;
using VoltaXApi.Dtos;

namespace VoltaXApi.Services
{
    public class OrderService : IOrderService
    {
        private readonly ICardRepository _cardRepository;
        private readonly IDebitCardRepository _debitCardRepository;
        private readonly IOrderRepository _orderRepository;

        public OrderService(ICardRepository cardRepository, IDebitCardRepository debitCardRepository, IOrderRepository orderRepository)
        {
            _cardRepository = cardRepository;
            _debitCardRepository = debitCardRepository;
            _orderRepository = orderRepository;
        }

        public async Task<bool> ProcessPayment(RechargeOrderDto orderDto)
        {
            var debitCard = new DebitCard
            {
                Name = orderDto.CardHolderName,
                CardNumber = orderDto.CardNumber,
                CVV = orderDto.CardCVV,
                ExpirationDate = DateTime.Parse(orderDto.CardExpirationDate),
                UserID = orderDto.UserID
            };
            // Here you would call your payment API to process the payment
            // If the payment is successful, you would then add the amount to the user's card balance
            // and save the debit card information if the user chose to do so

            // Process payment with payment API
            // This is a placeholder, replace with your actual payment processing code
            bool paymentSuccessful = true;

            if (paymentSuccessful)
            {
                // Add amount to user's card balance
                var card = await _cardRepository.GetByIdAsync(orderDto.CardID);
                if (card == null)
                {
                    throw new Exception("Card not found");
                }

                card.Balance += orderDto.RechargeAmount;
                await _cardRepository.Update(card);

                // Save debit card information if user chose to do so
                if (orderDto.SaveCard)
                {
                    await _debitCardRepository.AddAsync(debitCard);
                }

                // Create a new order
                var order = new Order
                {
                    CardID = card.ID,
                    Amount = orderDto.RechargeAmount,
                    RechargeDate = DateTime.Now
                };

                await _orderRepository.AddAsync(order);

                return true;
            }
            else
            {
                return false;
            }
        }


    }

}