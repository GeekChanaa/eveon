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
using VoltaXApi.Services;
using VoltaxApi.Dtos;

namespace VoltaXApi.Controllers
{

    [Route("api/partner/chargingSession")]
    [ApiController]
    public class PartnerChargingSessionController : GenericController<ChargingSession>
    {
        private readonly IChargingSessionRepository _repository;

        public PartnerChargingSessionController(
            IChargingSessionRepository repository,
            ChargingSessionInvoiceGeneratorService invoiceGeneratorService) : base(repository)
        {
            _repository = repository;
        }

        [HttpGet("GetChargingSessions/{partnerID}")]
        public async Task<List<PartnerChargingSessionListDto>> GetChargingSessions(int partnerID, [FromQuery] GlobalParams globalParams)
        {
            var chargingSessions = this._repository.GetPartnerChargingSessions(partnerID);
            var chargingSessionsList = await PagedList<PartnerChargingSessionListDto>.CreateAsync(chargingSessions, globalParams.PageNumber, globalParams.PageSize);
            Response.AddPagination(chargingSessionsList.CurrentPage, chargingSessionsList.PageSize, chargingSessionsList.TotalCount, chargingSessionsList.TotalPages);
            return chargingSessionsList;
        }


    }
}