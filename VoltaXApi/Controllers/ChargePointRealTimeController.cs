

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
        private readonly ChargePointStatusManagerService _chargePoints;
        public ChargePointRealTimeController(
          ChargePointStatusManagerService chargePoints
        )
        {
          _chargePoints = chargePoints;
        }

        /// <summary>Whether the charge point has an open connection, on this instance or another one (scale-out).</summary>
        [HttpGet("Status/{chargePointID}")]
        public IActionResult Status(string chargePointID)
        {
          bool isActive = _chargePoints.ChargePointExists(chargePointID);
          return Ok(new { ChargePointID = chargePointID, IsActive = isActive });
        }
    }
}