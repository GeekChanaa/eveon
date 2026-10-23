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
        private readonly IOrderRepository _orderRepository;
        private readonly IWalletService _walletService;
        private readonly VoltaXApi.Services.Billing.IBillingEvents _billing;

        public OrderService(
            ICardRepository cardRepository,
            IOrderRepository orderRepository,
            IWalletService walletService,
            VoltaXApi.Services.Billing.IBillingEvents billing)
        {
            _cardRepository = cardRepository;
            _orderRepository = orderRepository;
            _walletService = walletService;
            _billing = billing;
        }

        public async Task<Order> CreateManualTopUp(CreateRechargeOrderDto orderDto, int operatorUserId)
        {
            _ = await _cardRepository.GetByIdAsync(orderDto.CardID)
                ?? throw new CardNotFoundException($"There is no card with the id {orderDto.CardID}");
            var order = await _orderRepository.CreateRechargeOrder(orderDto);
            order.Status = RechargeOrderStatus.Completed;
            order.Provider = "Manual";
            order.PaidAt = DateTime.UtcNow;
            order.CreatedByUserID = operatorUserId;
            await _orderRepository.Update(order);
            await _walletService.PostAsync(order.CardID, new WalletPosting(order.Amount, WalletTransactionType.TopUp, $"order:{order.ID}:topup",
                $"Manual top-up: {orderDto.Note}", OrderID: order.ID, CreatedByUserID: operatorUserId));

            // Issues the legal invoice and emails it to the card owner.
            await _billing.TopUpCompletedAsync(order.ID);
            return order;
        }
    }
}
