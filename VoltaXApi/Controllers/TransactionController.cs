using VoltaXApi.Models;
using Microsoft.AspNetCore.Mvc;
using VoltaXApi.Data;
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
    public class TransactionController : GenericController<Transaction>
    {
        private readonly ITransactionRepository _repository;

        public TransactionController(ITransactionRepository repository) : base(repository)
        {
            _repository = repository;
        }

        [HttpGet("TotalEnergyConsumed")]
        public async Task<IActionResult> GetTotalEnergyConsumedAsync()
        {
            var result = await _repository.GetTotalEnergyConsumedAsync();
            return Ok(result);
        }

        [HttpGet("TotalEnergyConsumedToday")]
        public async Task<IActionResult> GetTotalEnergyConsumedTodayAsync()
        {
            var result = await _repository.GetTotalEnergyConsumedTodayAsync();
            return Ok(result);
        }

        [HttpGet("DailyEnergyConsumedLast30Days")]
        public async Task<IActionResult> GetDailyEnergyConsumedLast30DaysAsync()
        {
            var result = await _repository.GetDailyEnergyConsumedLast30DaysAsync();
            return Ok(result);
        }

        [HttpGet("MonthlyEnergyConsumedLastYear")]
        public async Task<IActionResult> GetMonthlyEnergyConsumedLastYearAsync()
        {
            var result = await _repository.GetMonthlyEnergyConsumedLastYearAsync();
            return Ok(result);
        }

        [HttpGet("GetTotalEnergyConsumedBetween")]
        public async Task<IActionResult> GetTotalEnergyConsumedBetween([FromQuery] DateTime dateStart , [FromQuery] DateTime dateEnd)
        {
            var result = await _repository.GetTotalEnergyConsumedBetween(dateStart ,dateEnd);
            return Ok(result);
        }

        [HttpGet("GetLatestTransactions")]
        public async Task<ActionResult<List<Transaction>>> GetLatestTransactions()
        {
            return await this._repository.GetLatestTransactions();
        }

        [HttpGet("GetCardTransactions/{cardID}")]
        public async Task<ActionResult<List<Transaction>>> GetCardTransactions(int cardID,[FromQuery] GlobalParams globalParams)
        {
            var cards = _repository.GetCardTransactions(cardID);
            var cardsList = await PagedList<Transaction>.CreateAsync(cards,globalParams.PageNumber, globalParams.PageSize);
            Response.AddPagination(cardsList.CurrentPage, cardsList.PageSize, cardsList.TotalCount, cardsList.TotalPages);
            return cardsList;
        }

        [HttpGet("GetChargePointTransactions/{chargePointId}")]
        public async Task<ActionResult<List<TransactionListDto>>> GetChargePointTransactions(string chargePointId)
        {
            return await this._repository.GetChargePointTransactions(chargePointId);
        }


        

        
        /****
            PARTNER TRANSACTIONS MANAGEMENT
        ***/

        [HttpGet("PartnerTotalEnergyConsumed/{partnerID}")]
        public async Task<IActionResult> GetPartnerTotalEnergyConsumedAsync(int partnerID)
        {
            var result = await _repository.GetPartnerTotalEnergyConsumedAsync(partnerID);
            return Ok(result);
        }

        [HttpGet("PartnerTotalEnergyConsumedToday/{partnerID}")]
        public async Task<IActionResult> GetPartnerTotalEnergyConsumedTodayAsync(int partnerID)
        {
            var result = await _repository.GetPartnerTotalEnergyConsumedTodayAsync(partnerID);
            return Ok(result);
        }

        [HttpGet("PartnerDailyEnergyConsumedLast30Days/{partnerID}")]
        public async Task<IActionResult> GetPartnerDailyEnergyConsumedLast30DaysAsync(int partnerID)
        {
            var result = await _repository.GetPartnerDailyEnergyConsumedLast30DaysAsync(partnerID);
            return Ok(result);
        }

        [HttpGet("PartnerMonthlyEnergyConsumedLastYear/{partnerID}")]
        public async Task<IActionResult> GetPartnerMonthlyEnergyConsumedLastYearAsync(int partnerID)
        {
            var result = await _repository.GetPartnerMonthlyEnergyConsumedLastYearAsync(partnerID);
            return Ok(result);
        }

        [HttpGet("GetPartnerTotalEnergyConsumedBetween/{partnerID}")]
        public async Task<IActionResult> GetPartnerTotalEnergyConsumedBetween(int partnerID,[FromQuery] DateTime dateStart , [FromQuery] DateTime dateEnd)
        {
            var result = await _repository.GetPartnerTotalEnergyConsumedBetween(partnerID,dateStart ,dateEnd);
            return Ok(result);
        }

        [HttpGet("GetPartnerLatestTransactions/{partnerID}")]
        public async Task<ActionResult<List<Transaction>>> GetPartnerLatestTransactions(int partnerID)
        {
            return await this._repository.GetPartnerLatestTransactions(partnerID);
        }
    }
}
