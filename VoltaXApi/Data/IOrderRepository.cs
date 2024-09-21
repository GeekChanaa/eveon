using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using VoltaXApi.Models;
using VoltaXApi.Helpers;
using VoltaXApi.Dtos;

namespace VoltaXApi.Data
{
    public interface IOrderRepository : IRepository<Order>
    {
        Task<double> CountRecharge(Expression<Func<Order, bool>> predicate);
        Task<InvoiceDTO> GetOrderForInvoice(int orderID);
        IQueryable<Order> GetCardOrders(int cardID);
    }
}