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

    [Route("api/partners/{partnerId}/[controller]")]
    [ApiController]
    public class PartnerConnectorStatusController : GenericController<ConnectorStatus>
    {
        private readonly IConnectorStatusRepository _repository;

        public PartnerConnectorStatusController(
            IConnectorStatusRepository repository) : base(repository)
        {
            _repository = repository;
        }

        [HttpGet("GetNumberOfPartnerConnectorsByStatus")]
        public async Task<ActionResult<int>> GetNumberOfConnectorsByStatus([FromQuery] string status)
        {
            return await this._repository.GetNumberOfConnectorsByStatus(status);
        }

        [HttpGet("GetNumberOfPartnerConnectorsByAllStatus")]
        public async Task<ActionResult<ConnectorStatusesDto>> GetNumberOfConnectorsByAllStatus(int partnerId)
        {
            return await this._repository.GetNumberOfPartnerConnectorsByAllStatus(partnerId);
        }
    }
}