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
    public class ConnectorController : GenericController<Connector>
    {
        private readonly IConnectorRepository _repository;

        public ConnectorController(IConnectorRepository repository) : base(repository)
        {
            _repository = repository;
        }

        [HttpGet("GetChargePointConnectors/{id}")]
        public async Task<IActionResult> GetChargePointConnectors(int id)
        {
            var entity = await this._repository.GetChargePointConnectors(id);
            if (entity == null)
            {
                return NotFound();
            }
            return Ok(entity);
        }

        [HttpGet("GetConnectorsIds")]
        public async Task<IActionResult> GetConnectorsIds()
        {
            return Ok(await this._repository.GetConnectorsIds());
        }

        [HttpGet("GetChargePointEvsesIds/{chargePointID}")]
        public async Task<IActionResult> GetChargePointEvsesIds(int chargePointID)
        {
            return Ok(await this._repository.GetChargePointEvsesIds(chargePointID));
        }

        [HttpPost("UpdateConnectorPricing/{connectorID}")]
        public async Task<IActionResult> UpdateConnectorPricing(int connectorID, UpdateConnectorPricingDto updateConnectorPricingDto)
        {
            return Ok(await this._repository.UpdateConnectorPricing(connectorID, updateConnectorPricingDto));
        }

        [HttpPost("UpdateConnectorFlatFee/{connectorID}")]
        public async Task<IActionResult> UpdateConnectorFlatFee(int connectorID, [FromBody] UpdateConnectorFlatFeeDto flatFee)
        {
            return Ok(await this._repository.UpdateConnectorFlatFee(connectorID, flatFee.FlatFee));
        }

        [HttpPost("ResetPricingChargePointConnectors/{chargePointID}")]
        public async Task<IActionResult> ResetPricingChargePointConnectors(int chargePointID)
        {
            await this._repository.ResetPricingChargePointConnectors(chargePointID);
            return StatusCode(201);
        }

        [HttpPost("ResetPricingConnector/{connectorID}")]
        public async Task<IActionResult> ResetPricingConnector(int connectorID)
        {
            await this._repository.ResetPricingConnector(connectorID);
            return StatusCode(201);
        }
        
    }
}