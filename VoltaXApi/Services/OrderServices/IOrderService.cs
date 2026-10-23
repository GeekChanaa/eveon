using VoltaXApi.Models;
using VoltaXApi.Dtos;

namespace VoltaXApi.Services
{
    public interface IOrderService
    {
        Task<Order> CreateManualTopUp(CreateRechargeOrderDto orderDto, int operatorUserId);
    }
}
