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

namespace VoltaXApi.Data
{
    public class OrderRepository : Repository<Order>,IOrderRepository
    {
        public OrderRepository(VoltaXApiDbContext context) : base(context)
        {
            
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

    }
}

