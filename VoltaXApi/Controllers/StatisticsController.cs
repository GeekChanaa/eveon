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
using VoltaXApi.Services;

namespace VoltaXApi.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class StatisticsController : Controller
    {
        private readonly IStatisticsService _statisticsService;
        private readonly GlobalConfigurations _globalConfigurations;

        public StatisticsController(
            IStatisticsService service,
            GlobalConfigurations globalConfigurations)
        {
            _statisticsService = service;
            _globalConfigurations = globalConfigurations;
        }

        [HttpGet("GetChargePointStatisticsSummary/{ChargePointID}")]
        public async Task<ActionResult<ChargePointStatisticsSummaryDto>> GetChargePointStatisticsSummary(int chargePointID)
        {
            return await _statisticsService.GetChargePointStatisticsSummary(chargePointID);
        }
        
        [HttpGet("TotalRevenue")]
        public async Task<IActionResult> GetTotalRevenueAsync()
        {
            var result = await _statisticsService.GetTotalRevenueAsync(_globalConfigurations.Vat, _globalConfigurations.GracePeriod);
            return Ok(result);
        }

        [HttpGet("TotalRevenueToday")]
        public async Task<IActionResult> GetTotalRevenueTodayAsync()
        {
            var result = await _statisticsService.GetTotalRevenueTodayAsync(_globalConfigurations.Vat, _globalConfigurations.GracePeriod);
            return Ok(result);
        }

        [HttpGet("DailyRevenueLast30Days")]
        public async Task<IActionResult> GetDailyRevenueLast30DaysAsync()
        {
            var result = await _statisticsService.GetDailyRevenueLast30DaysAsync(_globalConfigurations.Vat, _globalConfigurations.GracePeriod);
            return Ok(result);
        }

        [HttpGet("MonthlyRevenueLastYear")]
        public async Task<IActionResult> GetMonthlyRevenueLastYearAsync()
        {
            var result = await _statisticsService.GetMonthlyRevenueLastYearAsync(_globalConfigurations.Vat, _globalConfigurations.GracePeriod);
            return Ok(result);
        }
    }
}