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
    public class OCPPLocalListVersionController : GenericController<OCPPLocalListVersion>
    {
        private readonly IOCPPLocalListVersionRepository _repository;
        public OCPPLocalListVersionController(
            IOCPPLocalListVersionRepository repository) : base(repository)
        {
            _repository = repository;
        }

        [HttpGet("GetChargePointLocalList/{chargePointID}")]
        public async Task<ActionResult<OCPPLocalListDashboardDto>> GetChargePointLocalList(int chargePointID)
        {
            return await _repository.GetChargePointLocalList(chargePointID);
        }
    }
}