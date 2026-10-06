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
using VoltaXApi.Exceptions;

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
                UserName = ro.Card.User.FullName
            });
            return orders;
        }

        public IQueryable<RechargeOrderListDto> GetUserRechargeOrders(int userID, GlobalParams globalParams)
        {
            var orders = GetAllAsync(globalParams).Where(u => u.Card.UserID == userID).Select(ro => new RechargeOrderListDto{
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

        public async Task<InvoiceData> GetOrderForInvoice(int orderID)
        {
            return await  _context.Orders.Where(u => u.ID == orderID).Select(o => new InvoiceData{
                Date = o.RechargeDate.ToString("dd MMM yyyy"),
                InvoiceNumber = o.ID.ToString(),
                CardNumber = o.Card.CardNumber,
                BilledTo = $"{o.Card.User.FirstName} {o.Card.User.LastName}",
                TotalAmount = o.Amount,
                AmountHT = o.Amount - o.Amount*0.8,
                VAT = o.Amount - o.Amount*0.2
            }).FirstOrDefaultAsync();
        }

        public IQueryable<Order> GetCardOrders(int cardID)
        {
            return _context.Orders.Where(t => t.CardID == cardID);
        }

        public async Task<Order> CreateRechargeOrder(CreateRechargeOrderDto rechargeOrderDto)
        {
            Order order = _mapper.Map<CreateRechargeOrderDto,Order>(rechargeOrderDto);
            await AddAsync(order);
            return order;
        }

        public async Task<DisplayRechargeOrderDto> GetOrder(int orderID)
        {
            DisplayRechargeOrderDto order = await dbSet.Select(o => new DisplayRechargeOrderDto{
                ID = o.ID,
                CardID = o.CardID,
                CardNumber = o.Card.CardNumber,
                Amount = o.Amount,
                RechargeDate = o.RechargeDate,
                UserName = o.Card.User.FullName,
                Status = o.Status
            }).FirstOrDefaultAsync(u => u.ID == orderID);
            if(order == null) throw new NotFoundException("Order With ID Not Found");

            return order;
        }



    }
}

