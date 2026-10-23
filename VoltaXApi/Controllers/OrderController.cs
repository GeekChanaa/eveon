using System.Security.Claims;
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
using VoltaXApi.Helpers;

namespace VoltaXApi.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class OrderController : GenericController<Order>
    {
        private readonly IOrderRepository _repository;
        private readonly IOrderService _orderService;
        private readonly IOrderStatisticsService _statistics;
        private readonly IInvoiceGeneratorService<InvoiceData> _invoiceService;

        public OrderController(
            IOrderRepository repository,
            IInvoiceGeneratorService<InvoiceData> invoiceService,
            IOrderService orderService,
            IOrderStatisticsService statistics) : base(repository)
        {
            _repository = repository;
            _invoiceService = invoiceService;
            _orderService = orderService;
            _statistics = statistics;
        }

        [HttpGet("countRechargeAmount")]
        public async Task<IActionResult> CountRecharge() => Ok(await _statistics.RechargedAmountAsync());

        [HttpGet("GetRechargeOrders")]
        public async Task<List<RechargeOrderListDto>> GetRechargeOrders([FromQuery] GlobalParams globalParams)
        {
            var partners = this._repository.GetRechargeOrders(globalParams);
            var partnersList = await PagedList<RechargeOrderListDto>.CreateAsync(partners,globalParams.PageNumber, globalParams.PageSize);
            Response.AddPagination(partnersList.CurrentPage, partnersList.PageSize, partnersList.TotalCount, partnersList.TotalPages);
            return partnersList;
        }

        [HttpGet("countRechargeAmountToday")]
        public async Task<IActionResult> CountRechargeToday() => Ok(await _statistics.RechargedAmountTodayAsync());

        [HttpGet("countRechargeAmountBetween")]
        public async Task<IActionResult> CountRechargeBetween([FromQuery] DateTime dateStart, [FromQuery] DateTime dateEnd) =>
            Ok(await _statistics.RechargedAmountBetweenAsync(dateStart, dateEnd));

        // The last 30 business days, oldest first.
        [HttpGet("countRechargeAmountByDay")]
        public async Task<IActionResult> CountRechargeAmountByDay() => Ok(await _statistics.RechargedAmountPerDayAsync(30));

        [HttpGet("countToday")]
        public async Task<IActionResult> CountToday() => Ok((double)await _statistics.CountTodayAsync());

        [HttpGet("countByDay")]
        public async Task<IActionResult> CountByDay() => Ok(await _statistics.CountPerDayAsync(30));

        [HttpGet("countByLast7Days")]
        public async Task<IActionResult> CountByLast7Days() => Ok(await _statistics.CountPerDayAsync(7));

        // The last 12 business months including the current one, oldest first.
        [HttpGet("countByLast12Months")]
        public async Task<IActionResult> CountByLast12Months() => Ok(await _statistics.CountPerMonthAsync(12));

        /// <summary>
        /// Manual top-up by an operator (cash, bank transfer...): credited at once, with the operator and a
        /// reason on record. Customers pay online through api/payments.
        /// </summary>
        [HttpPost("CreateRechargeOrder")]
        public async Task<IActionResult> CreateRechargeOrder(CreateRechargeOrderDto rechargeOrderDto)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var operatorId))
                return Unauthorized();
            var order = await this._orderService.CreateManualTopUp(rechargeOrderDto, operatorId);
            return Ok(new { orderId = order.ID });
        }


        // Get Invoice PDF Info 
        [HttpGet("GetInvoiceInfo/{orderID}")]
        public async Task<IActionResult> GetInvoiceInfo(int orderID)
        {
            return Ok(await this._repository.GetOrderForInvoice(orderID));
        }

        [HttpGet("GetOrder/{orderID}")]
        public async Task<IActionResult> GetOrder(int orderID)
        {
            return Ok(await this._repository.GetOrder(orderID));
        }

        [HttpGet("GetCardOrders/{cardID}")]
        public async Task<ActionResult<List<Order>>> GetCardOrders(int cardID,[FromQuery] GlobalParams globalParams)
        {
            var orders = _repository.GetCardOrders(cardID);
            var ordersList = await PagedList<Order>.CreateAsync(orders,globalParams.PageNumber, globalParams.PageSize);
            Response.AddPagination(ordersList.CurrentPage, ordersList.PageSize, ordersList.TotalCount, ordersList.TotalPages);
            return ordersList;
        }

        [HttpGet("GetUserOrders/{userID}")]
        public async Task<ActionResult<PagedList<RechargeOrderListDto>>> GetUserOrders(int userID,[FromQuery] GlobalParams globalParams)
        {
            var orders = _repository.GetUserRechargeOrders(userID,globalParams);
            var ordersList = await PagedList<RechargeOrderListDto>.CreateAsync(orders,globalParams.PageNumber, globalParams.PageSize);
            Response.AddPagination(ordersList.CurrentPage, ordersList.PageSize, ordersList.TotalCount, ordersList.TotalPages);
            return ordersList;
        }

        [HttpGet("GetOrderInvoice/{orderID}")]
        public async Task<IActionResult> GetOrderInvoice(int orderID)
        {
            InvoiceData? orderInvoiceInformations = await _repository.GetOrderForInvoice(orderID);
            if (orderInvoiceInformations == null)
                return NotFound();
            var pdfBytes = _invoiceService.GenerateInvoice(orderInvoiceInformations);
            return File(pdfBytes, "application/pdf", $"Invoice-{orderID}.pdf");
        }
    }
}
