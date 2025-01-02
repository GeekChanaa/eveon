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
    public class ChargingSessionController : GenericController<ChargingSession>
    {
        private readonly IChargingSessionRepository _repository;

        public ChargingSessionController(IChargingSessionRepository repository) : base(repository)
        {
            _repository = repository;
        }

        [HttpGet("GetChargePointChargingSessions/{chargePointID}")]
        public async Task<List<ChargePointChargingSessionListDto>> GetChargePointChargingSessions(int chargePointID, [FromQuery] GlobalParams globalParams)
        {
            var chargingSessions = this._repository.GetChargePointChargingSessions(chargePointID);
            var chargingSessionsList = await PagedList<ChargePointChargingSessionListDto>.CreateAsync(chargingSessions,globalParams.PageNumber, globalParams.PageSize);
            Response.AddPagination(chargingSessionsList.CurrentPage, chargingSessionsList.PageSize, chargingSessionsList.TotalCount, chargingSessionsList.TotalPages);
            return chargingSessionsList;
        }

        


    }
}