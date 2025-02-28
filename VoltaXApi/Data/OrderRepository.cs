using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using VoltaXApi.Helpers;
using VoltaXApi.Models;
using VoltaXApi.Dtos;
using AutoMapper;
using AutoMapper.QueryableExtensions;

namespace VoltaXApi.Data
{
    public class OrderRepository : Repository<Order>,IOrderRepository
    {
        private readonly IMapper _mapper;
        public OrderRepository(
            VoltaXApiDbContext context,
            IMapper mapper) : base(context)
        {
            _mapper = mapper;
        }

        public IQueryable<RechargeOrderListDto> GetRechargeOrders(GlobalParams globalParams)
        {
            var orders = GetAllAsync(globalParams).Select(ro => new RechargeOrderListDto{
                ID = ro.ID,
                CardID = ro.CardID,
                Amount = ro.Amount,
                CardNumber = ro.Card.CardNumber,
                Status = ro.Status,
                RechargeDate = ro.RechargeDate,
            });
            return orders;
        }


        public Task<double> CountRecharge(Expression<Func<Order, bool>> predicate)
        {
            var Amount = _context.Orders
                .Where(predicate)
                .SumAsync(t => t.Amount);

            return Amount;
        }

        public async Task<InvoiceDTO> GetOrderForInvoice(int orderID)
        {
            var order = _context.Orders.Include(u => u.Card).ThenInclude(u => u.User).FirstOrDefault(u => u.ID == orderID);
            var invoice = new InvoiceDTO
            {
                OrderNumber = order.ID.ToString(),
                BilledTo = $"{order.Card.User.FirstName} {order.Card.User.LastName}",
                PayTo = "VoltaX Charging",
                PaymentMethod = "CMI",
                Phone = order.Card.User.Phone,
                Email = order.Card.User.Email,
                CardID = order.Card.CardNumber,
                Date = order.RechargeDate.ToString("dd MMM yyyy"),
            };

            return invoice;
        }

        public IQueryable<Order> GetCardOrders(int cardID)
        {
            return _context.Orders.Where(t => t.CardID == cardID);
        }

        public async Task CreateRechargeOrder(CreateRechargeOrderDto rechargeOrderDto)
        {
            Order order = _mapper.Map<CreateRechargeOrderDto,Order>(rechargeOrderDto);
            await AddAsync(order);
        }


    }
}

