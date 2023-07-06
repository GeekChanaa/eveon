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
    public class ChargePointController : GenericController<ChargePoint>
    {
        private readonly IChargePointRepository _repository;

        public ChargePointController(IChargePointRepository repository) : base(repository)
        {
            _repository = repository;
        }

        // Get ChargePoint Connectors
        [HttpGet("GetChargePointConnectors")]
        public async Task<ActionResult<List<Connector>>> GetChargePointConnectors([FromQuery] int chargePointID)
        {
            return await this._repository.GetChargePointConnectors(chargePointID);
        }

        [HttpPost]
        public override async Task<IActionResult> Create(ChargePoint chargePoint)
        {
            if (chargePoint == null)
            {
                return BadRequest("Entity is null");
            }

            try
            {
                await _repository.AddAsync(chargePoint);
            }
            catch (Exception ex)
            {
                // Handle any exception that was thrown in the AddAsync method
                // You can replace this with more specific error handling if you want
                return BadRequest(ex.Message);
            }

            return CreatedAtAction("GetById", new { id = chargePoint.ID }, chargePoint);
        }
    }
}