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
    [Route("api/partner/statistics")]
    [ApiController]
    public class PartnerStatisticsController : Controller
    {
        private readonly IPartnerStatisticsService _statisticsService;
        private readonly GlobalConfigurations _globalConfigurations;

        public PartnerStatisticsController(
            IPartnerStatisticsService service,
            GlobalConfigurations globalConfigurations)
        {
            _statisticsService = service;
            _globalConfigurations = globalConfigurations;
        }

        // Existing Revenue endpoints
        [HttpGet("TotalRevenue/{partnerID}")]
        public async Task<IActionResult> GetTotalRevenueAsync(int partnerID)
        {
            var result = await _statisticsService.GetTotalRevenueForPartnerAsync(partnerID, _globalConfigurations.Vat, _globalConfigurations.GracePeriod);
            return Ok(result);
        }

        [HttpGet("TotalRevenueToday/{partnerID}")]
        public async Task<IActionResult> GetTotalRevenueTodayAsync(int partnerID)
        {
            var result = await _statisticsService.GetTotalRevenueForPartnerTodayAsync(partnerID, _globalConfigurations.Vat, _globalConfigurations.GracePeriod);
            return Ok(result);
        }

        [HttpGet("DailyRevenueLast30Days/{partnerID}")]
        public async Task<IActionResult> GetDailyRevenueLast30DaysAsync(int partnerID)
        {
            var result = await _statisticsService.GetDailyRevenueForPartnerLast30DaysAsync(partnerID, _globalConfigurations.Vat, _globalConfigurations.GracePeriod);
            return Ok(result);
        }

        [HttpGet("MonthlyRevenueLastYear/{partnerID}")]
        public async Task<IActionResult> GetMonthlyRevenueLastYearAsync(int partnerID)
        {
            var result = await _statisticsService.GetMonthlyRevenueForPartnerLastYearAsync(partnerID, _globalConfigurations.Vat, _globalConfigurations.GracePeriod);
            return Ok(result);
        }

        // Charging Sessions endpoints
        [HttpGet("TotalChargingSessions/{partnerID}")]
        public async Task<IActionResult> GetTotalChargingSessionsAsync(int partnerID)
        {
            var result = await _statisticsService.GetTotalChargingSessionsForPartnerAsync(partnerID);
            return Ok(result);
        }

        [HttpGet("TotalChargingSessionsToday/{partnerID}")]
        public async Task<IActionResult> GetTotalChargingSessionsTodayAsync(int partnerID)
        {
            var result = await _statisticsService.GetTotalChargingSessionsForPartnerTodayAsync(partnerID);
            return Ok(result);
        }

        [HttpGet("DailyChargingSessionsLast30Days/{partnerID}")]
        public async Task<IActionResult> GetDailyChargingSessionsLast30DaysAsync(int partnerID)
        {
            var result = await _statisticsService.GetDailyChargingSessionsForPartnerLast30DaysAsync(partnerID);
            return Ok(result);
        }

        [HttpGet("MonthlyChargingSessionsLastYear/{partnerID}")]
        public async Task<IActionResult> GetMonthlyChargingSessionsLastYearAsync(int partnerID)
        {
            var result = await _statisticsService.GetMonthlyChargingSessionsForPartnerLastYearAsync(partnerID);
            return Ok(result);
        }

        // New Charged Minutes endpoints
        [HttpGet("TotalChargedMinutes/{partnerID}")]
        public async Task<IActionResult> GetTotalChargedMinutesAsync(int partnerID)
        {
            var result = await _statisticsService.GetTotalChargedMinutesForPartnerAsync(partnerID);
            return Ok(result);
        }

        [HttpGet("TotalChargedMinutesToday/{partnerID}")]
        public async Task<IActionResult> GetTotalChargedMinutesTodayAsync(int partnerID)
        {
            var result = await _statisticsService.GetTotalChargedMinutesForPartnerTodayAsync(partnerID);
            return Ok(result);
        }

        [HttpGet("DailyChargedMinutesLast30Days/{partnerID}")]
        public async Task<IActionResult> GetDailyChargedMinutesLast30DaysAsync(int partnerID)
        {
            var result = await _statisticsService.GetDailyChargedMinutesForPartnerLast30DaysAsync(partnerID);
            return Ok(result);
        }

        [HttpGet("MonthlyChargedMinutesLastYear/{partnerID}")]
        public async Task<IActionResult> GetMonthlyChargedMinutesLastYearAsync(int partnerID)
        {
            var result = await _statisticsService.GetMonthlyChargedMinutesForPartnerLastYearAsync(partnerID);
            return Ok(result);
        }

        // New Energy Consumed endpoints
        [HttpGet("TotalEnergyConsumed/{partnerID}")]
        public async Task<IActionResult> GetTotalEnergyConsumedAsync(int partnerID)
        {
            var result = await _statisticsService.GetTotalEnergyConsumedForPartnerAsync(partnerID);
            return Ok(result);
        }

        [HttpGet("TotalEnergyConsumedToday/{partnerID}")]
        public async Task<IActionResult> GetTotalEnergyConsumedTodayAsync(int partnerID)
        {
            var result = await _statisticsService.GetTotalEnergyConsumedForPartnerTodayAsync(partnerID);
            return Ok(result);
        }

        [HttpGet("DailyEnergyConsumedLast30Days/{partnerID}")]
        public async Task<IActionResult> GetDailyEnergyConsumedLast30DaysAsync(int partnerID)
        {
            var result = await _statisticsService.GetDailyEnergyConsumedForPartnerLast30DaysAsync(partnerID);
            return Ok(result);
        }

        [HttpGet("MonthlyEnergyConsumedLastYear/{partnerID}")]
        public async Task<IActionResult> GetMonthlyEnergyConsumedLastYearAsync(int partnerID)
        {
            var result = await _statisticsService.GetMonthlyEnergyConsumedForPartnerLastYearAsync(partnerID);
            return Ok(result);
        }
    }
}
