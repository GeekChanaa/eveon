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
using VoltaXApi.Helpers;

namespace VoltaXApi.Controllers
{
    [Route("api/partner/chargingStation")]
    [ApiController]
    public class PartnerChargingStationController : Controller
    {
        private readonly GlobalConfigurations _globalConfigurations;
        private readonly IChargingStationRepository _chargingStationRepository;

        public PartnerChargingStationController(
            GlobalConfigurations globalConfigurations,
            IChargingStationRepository chargingStationRepository)
        {
            _globalConfigurations = globalConfigurations;
            _chargingStationRepository = chargingStationRepository;
        }

        // Existing Revenue endpoints
        [HttpGet("GetPartnerTop10ChargingStations/{partnerID}")]
        public async Task<PagedList<ChargingStationRevenue>> GetTotalRevenueAsync(int partnerID, [FromQuery] GlobalParams globalParams)
        {
            var chargingStations = (await _chargingStationRepository.GetPartnerTop10ChargingStationsByRevenue(partnerID, globalParams)).ToList();

            var result = PagedList<ChargingStationRevenue>.Create(chargingStations,globalParams.PageNumber, globalParams.PageSize);
            Response.AddPagination(result.CurrentPage, result.PageSize, result.TotalCount, result.TotalPages);
            return result;
        }
    }
}
