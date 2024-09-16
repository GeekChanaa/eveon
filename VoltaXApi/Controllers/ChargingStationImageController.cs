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
    public class ChargingStationImageController : GenericController<ChargingStationImage>
    {
        private readonly IChargingStationImageRepository _repository;

        public ChargingStationImageController(IChargingStationImageRepository repository) : base(repository)
        {
            _repository = repository;
        }

        [HttpGet("GetChargingStationImages/{chargingStationID}")]
        public async Task<IActionResult> GetChargingStationImages(int chargingStationID)
        {
          var images = await this._repository.GetChargingStationImages(chargingStationID);
          return Ok(images);
        }
    }
}