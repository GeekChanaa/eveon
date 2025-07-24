using VoltaXApi.Models;
using VoltaXApi.Dtos;

namespace VoltaXApi.Services
{
    public interface IOrderService
    {
        Task<bool> ProcessPayment(RechargeOrderDto orderDto);
        Task CreateOrder(CreateRechargeOrderDto orderDto);
    }
}