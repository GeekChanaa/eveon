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
using VoltaXApi.Services;

namespace VoltaXApi.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class ChargingStationImageController : GenericController<ChargingStationImage>
    {
        private readonly IChargingStationImageRepository _repository;
        private readonly IChargingStationImageService _chargingStationImageService;

        public ChargingStationImageController(
            IChargingStationImageRepository repository,
            IChargingStationImageService service) : base(repository)
        {
            _repository = repository;
            _chargingStationImageService = service;
        }

        [HttpGet("GetChargingStationImages/{chargingStationID}")]
        public async Task<IActionResult> GetChargingStationImages(int chargingStationID)
        {
          var images = await this._repository.GetChargingStationImages(chargingStationID);
          return Ok(images);
        }

        [HttpPost("UploadChargingStationImages/{chargingStationID}")]
        public async Task<IActionResult> UploadChargingStationImages(int chargingStationID)
        {
          var ChargingStationImages = Request.Form.Files.Where(f => f.Name.Contains("chargingStationImages"));
              
          await this._chargingStationImageService.UploadChargingStationImages(ChargingStationImages,chargingStationID);
          return StatusCode(200);
        }

        
    }
}