using VoltaXApi.Models;
using Microsoft.AspNetCore.Mvc;
using VoltaXApi.Data;
using VoltaXApi.Services;
using VoltaXApi.Dtos;
using System.Threading.Tasks;
using System.Text;
using Microsoft.Extensions.Configuration;
using System;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Net.Http;
using System.Net;

namespace VoltaXApi.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : GenericController<Order>
    {
        private readonly IOrderRepository _repository;
        private readonly IOrderService _orderService;

        public OrderController(IOrderRepository repository) : base(repository)
        {
            _repository = repository;
        }

        // You can override the base methods or add specific methods for this controller
        [HttpGet("countRechargeAmount")]
        public async Task<IActionResult> CountRecharge()
        {
            double count = await _repository.CountRecharge(u => true);
            return Ok(count);
        }

        [HttpGet("countRechargeAmountToday")]
        public async Task<IActionResult> CountRechargeToday()
        {
            DateTime today = DateTime.Today;
            DateTime tomorrow = today.AddDays(1);

            double count = await _repository.CountRecharge(u => u.RechargeDate >= today && u.RechargeDate < tomorrow);
            return Ok(count);
        }

        [HttpGet("countRechargeAmountBetween")]
        public async Task<IActionResult> CountRechargeBetween([FromQuery] DateTime dateStart, [FromQuery] DateTime dateEnd)
        {
            double count = await _repository.CountRecharge(u => u.RechargeDate >= dateStart && u.RechargeDate <= dateEnd);
            return Ok(count);
        }

        [HttpGet("countRechargeAmountByDay")]
        public async Task<IActionResult> CountRechargeAmountByDay()
        {
            DateTime endDate = DateTime.Today;
            DateTime startDate = endDate.AddDays(-29);

            var rechargeAmountByDay = new List<double>();

            for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
            {
                DateTime currentDay = date.Date;
                DateTime nextDay = currentDay.AddDays(1);

                double rechargeAmount = await _repository
                    .CountRecharge(u => u.RechargeDate >= currentDay && u.RechargeDate < nextDay);

                rechargeAmountByDay.Add(rechargeAmount);
            }

            return Ok(rechargeAmountByDay);
        }

        [HttpGet("countToday")]
        public async Task<IActionResult> CountToday()
        {
            DateTime today = DateTime.Today;
            DateTime tomorrow = today.AddDays(1);
            double count = await _repository.CountAsync(u => u.RechargeDate >= today && u.RechargeDate < tomorrow);
            return Ok(count);
        }

        [HttpGet("countByDay")]
        public async Task<IActionResult> CountByDay()
        {
            DateTime endDate = DateTime.Today;
            DateTime startDate = endDate.AddDays(-29);

            var orderCountByDay = new List<double>();

            for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
            {
                DateTime currentDay = date.Date;
                DateTime nextDay = currentDay.AddDays(1);

                double rechargeAmount = await _repository
                    .CountAsync(u => u.RechargeDate >= currentDay && u.RechargeDate < nextDay);

                orderCountByDay.Add(rechargeAmount);
            }

            return Ok(orderCountByDay);
        }

        [HttpGet("countByLast7Days")]
        public async Task<IActionResult> CountByLast7Days()
        {
            DateTime endDate = DateTime.Today;
            DateTime startDate = endDate.AddDays(-6); // subtract 6 to include today in the 7 day count

            var orderCountByDay = new List<double>();

            for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
            {
                DateTime currentDay = date.Date;
                DateTime nextDay = currentDay.AddDays(1);

                double rechargeAmount = await _repository
                    .CountAsync(u => u.RechargeDate >= currentDay && u.RechargeDate < nextDay);

                orderCountByDay.Add(rechargeAmount);
            }

            return Ok(orderCountByDay);
        }

        [HttpGet("countByLast12Months")]
        public async Task<IActionResult> CountByLast12Months()
        {
            DateTime endDate = DateTime.Today;
            DateTime startDate = endDate.AddYears(-1).AddMonths(1); // subtract a year and add a month to include the current month in the 12 month count

            var orderCountByMonth = new List<double>();

            for (DateTime month = startDate; month <= endDate; month = month.AddMonths(1))
            {
                DateTime currentMonthStart = new DateTime(month.Year, month.Month, 1);
                DateTime nextMonthStart = currentMonthStart.AddMonths(1);

                double rechargeAmount = await _repository
                    .CountAsync(u => u.RechargeDate >= currentMonthStart && u.RechargeDate < nextMonthStart);

                orderCountByMonth.Add(rechargeAmount);
            }

            return Ok(orderCountByMonth);
        }

        [HttpPost("RechargeCard")]
        public async Task<IActionResult> RechargeCard(RechargeOrderDto rechargeOrderDto)
        {
            await this._orderService.ProcessPayment(rechargeOrderDto);
            return StatusCode(200);
        }


    }
}