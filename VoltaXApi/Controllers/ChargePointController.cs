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
using OCPP.Core.Server;
using VoltaXApi.Services;
using System.Net.WebSockets;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Services;
using System.Text.Json;
using System.Text.Json.Serialization;


namespace VoltaXApi.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class ChargePointController : GenericController<ChargePoint>
    {
        private readonly IChargePointRepository _repository;
        private readonly WebSocketManagerService _wsService;

        public ChargePointController(
            IChargePointRepository repository,
            WebSocketManagerService wsService) : base(repository)
        {
            _repository = repository;
            _wsService = wsService;
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
                return BadRequest(ex.Message);
            }

            return Ok(new { id = chargePoint.ID });
        }

        // Charging Stations of partner
        [HttpGet("GetPartnerChargePoints/{partnerID}")]
        public async Task<ActionResult<List<ChargingStation>>> GetPartnerChargePoints(int partnerID, [FromQuery] GlobalParams globalParams)
        {
            var chargingStations = await PagedList<ChargePoint>.CreateAsync((await _repository.GetAllAsync(globalParams)).Where(u => u.ChargingStation.PartnerID == partnerID), globalParams.PageNumber, globalParams.PageSize);
            Response.AddPagination(chargingStations.CurrentPage, chargingStations.PageSize, chargingStations.TotalCount, chargingStations.TotalPages);
            return Ok(chargingStations);
        }

        [HttpGet("{id}")]
        public override async Task<IActionResult> GetById(int id)
        {
            var entity = await this._repository.GetChargePointByIdAsync(id);
            if (entity == null)
            {
                return NotFound();
            }
            return Ok(entity);
        }

        [HttpGet("GetChargingStationChargePoints/{id}")]
        public async Task<IActionResult> GetChargingStationChargePoints(int id)
        {
            var entity = await this._repository.GetChargingStationChargePoints(id);
            if (entity == null)
            {
                return NotFound();
            }
            return Ok(entity);
        }

        [HttpGet("IsChargePointIDUnique/{chargePointID}")]
        public async Task<ActionResult<bool>> IsChargePointIDUnique(string chargePointID)
        {
            return await this._repository.IsChargePointIDUnique(chargePointID);
        }

        [HttpGet("IsChargePointSerialNumberUnique/{chargePointSerialNumber}")]
        public async Task<ActionResult<bool>> IsChargePointSerialNumberUnique(string chargePointSerialNumber)
        {
            return await this._repository.IsChargePointSerialNumberUnique(chargePointSerialNumber);
        }

        [HttpGet("GetChargePointByID/{chargePointID}")]
        public async Task<IActionResult> GetChargePointByID(int chargePointID)
        {
            var helper = new ChargePointIncludableHelper { };
            return Ok(await this._repository.GetChargePointByID(chargePointID, helper));
        }

        [HttpGet("GetChargePointsIds")]
        public async Task<IActionResult> GetChargePointsIds()
        {
            return Ok(await this._repository.GetChargePointsIds());
        }

        [HttpPost("{chargePointId}/sendMessage")]
        public async Task<IActionResult> SendMessageToChargePoint(string chargePointId, [FromBody] string message)
        {
            Console.WriteLine("searching for chargepointID : "+ chargePointId);
            var webSocket = _wsService.GetWebSocket(chargePointId);

            if (webSocket == null || webSocket.State != WebSocketState.Open)
            {
                return NotFound("WebSocket connection for this charge point is not available.");
            }
            
            await _wsService.SendMessageAsync(chargePointId, message);

            return Ok("Message sent successfully.");
        }

        [HttpGet("logging")]
        public async Task<IActionResult> Logging([FromBody] string message)
        {
            return StatusCode(200);
        }

        [HttpPut("SetShowOnMap/{chargePointID}")]
        public async Task<IActionResult> SetShowOnMap(int chargePointID, [FromBody] bool val)
        {
            await this._repository.SetShowOnMap(chargePointID, val);
            return StatusCode(200);
        }

        [HttpPut("SetHasChargeCable/{chargePointID}")]
        public async Task<IActionResult> SetHasChargeCable(int chargePointID, [FromBody] bool val)
        {
            await this._repository.SetHasChargeCable(chargePointID, val);
            return StatusCode(200);
        }


        // setShowOnMap
        // setHasChargeCable




    }
}