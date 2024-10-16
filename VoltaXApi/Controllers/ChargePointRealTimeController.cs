

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
using AutoMapper;
using VoltaXApi.OCPP.Services;

namespace VoltaXApi.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class ChargePointRealTimeController : ControllerBase
    {
        private readonly WebSocketManagerService _wsManagerService;
        public ChargePointRealTimeController(
          WebSocketManagerService wsManagerService
        )
        {
          _wsManagerService = wsManagerService;
        }

        [HttpGet("Status/{chargePointID}")]
        public async Task<IActionResult> Status(string chargePointID)
        {
          bool isActive = _wsManagerService.GetWebSocketStatus(chargePointID);
          return Ok(new { ChargePointID = chargePointID, IsActive = isActive });
        }
    }
}