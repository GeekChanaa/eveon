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
    }
}
