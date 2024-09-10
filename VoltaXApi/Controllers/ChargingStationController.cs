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
using Microsoft.AspNetCore.Authorization;
using VoltaXApi.Helpers;
using VoltaXApi.Services;

namespace VoltaXApi.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class ChargingStationController : GenericController<ChargingStation>
    {
        private readonly IChargingStationRepository _repository;
        private readonly IChargingStationService _chargingStationService;

        public ChargingStationController(
                IChargingStationService chargingStationService,
                IChargingStationRepository repository) : base(repository)
        {
            _repository = repository;
            _chargingStationService = chargingStationService;
        }

        [HttpGet("{id}")]
        public override async Task<IActionResult> GetById(int id)
        {
            var entity = await this._repository.GetChargingStationByIdAsync(id);
            if (entity == null)
            {
                return NotFound();
            }
            return Ok(entity);
        }

        [HttpGet("GetChargingStationForDisplay/{id}")]
        public async Task<IActionResult> GetChargingStationForDisplay(int id)
        {
            var helper = new ChargingStationIncludableHelper{
                includeImages = true
            };
            var entity = await this._repository.GetChargingStationByIdAsync(id, helper);
            if (entity == null)
            {
                return NotFound();
            }
            return Ok(entity);
        }

        [HttpGet("GetChargingStationRevenue")]
        public async Task<ActionResult<double>> GetChargingStationRevenue([FromQuery] int chargingStationID, string? start = null, string? end = null)
        {
            if (String.IsNullOrEmpty(start) || String.IsNullOrEmpty(end))
            {
                return await this._repository.GetChargingStationRevenue(chargingStationID);
            }

            DateTime? startDate = DateTime.Parse(start);
            DateTime? endDate = DateTime.Parse(end);

            return await this._repository.GetChargingStationRevenue(chargingStationID, startDate, endDate);
        }

        [HttpGet("GetChargingStationRevenueLast7Days")]
        public async Task<ActionResult<List<double>>> GetChargingStationRevenueLast7Days([FromQuery] int chargingStationID)
        {
            return (await _repository.GetChargingStationRevenueLast7Days(chargingStationID)).ToList();
        }

        [HttpGet("GetChargingStationRevenueLast30Days")]
        public async Task<ActionResult<List<double>>> GetChargingStationRevenueLast30Days([FromQuery] int chargingStationID)
        {
            return (await _repository.GetChargingStationRevenueLast30Days(chargingStationID)).ToList();
        }

        [HttpGet("GetChargingStationRevenueLast12Months")]
        public async Task<ActionResult<List<double>>> GetChargingStationRevenueLast12Months([FromQuery] int chargingStationID)
        {
            return (await _repository.GetChargingStationRevenueLast12Months(chargingStationID)).ToList();
        }


        [HttpGet("GetTop10ChargingStationsByRevenue")]
        public async Task<IActionResult> GetTop10ChargingStationsByRevenue()
        {
            return Ok(await _repository.GetTop10ChargingStationsByRevenue());
        }

        [HttpPost("Add")]
        public async Task<IActionResult> Create([FromForm] ChargingStationCreateDto chargingStationDto)
        {
            if (chargingStationDto == null)
            {
                return BadRequest("Entity is null");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var ChargingStationImages = Request.Form.Files.Where(f => f.Name.Contains("chargingStationImages"));
                chargingStationDto.ChargingStationImages = ChargingStationImages;

                var chargingStation =  await this._chargingStationService.CreateChargingStationWithDetails(chargingStationDto);
                
                return Ok(new { id = chargingStation.ID });
            }
            catch (Exception ex)
            {
                if(ex.InnerException != null)
                    Console.WriteLine(ex.InnerException);
                return BadRequest(ex.Message);
            }

        }

        // Charging Stations of partner
        [HttpGet("GetPartnerChargingStations/{partnerID}")]
        public async Task<ActionResult<List<ChargingStation>>> GetPartnerChargingStations(int partnerID , [FromQuery] GlobalParams globalParams)
        {
            var chargingStations = await PagedList<ChargingStation>.CreateAsync((await _repository.GetAllAsync(globalParams)).Where(u => u.PartnerID == partnerID), globalParams.PageNumber, globalParams.PageSize);
            Response.AddPagination(chargingStations.CurrentPage, chargingStations.PageSize, chargingStations.TotalCount, chargingStations.TotalPages);
            return Ok(chargingStations);
        }


        [HttpGet("GetPartnerChargingStationRevenue/{partnerID}")]
        public async Task<ActionResult<double>> GetPartnerChargingStationRevenue(int partnerID,[FromQuery] int chargingStationID, string? start = null, string? end = null)
        {
            if (String.IsNullOrEmpty(start) || String.IsNullOrEmpty(end))
            {
                Console.WriteLine("this is inside the if");
                return await this._repository.GetPartnerChargingStationRevenue(partnerID,chargingStationID);
            }


            DateTime? startDate = DateTime.Parse(start);
            DateTime? endDate = DateTime.Parse(end);

            return await this._repository.GetPartnerChargingStationRevenue(partnerID,chargingStationID, startDate, endDate);
        }

        [HttpGet("GetPartnerChargingStationRevenueLast7Days/{partnerID}")]
        public async Task<ActionResult<List<double>>> GetPartnerChargingStationRevenueLast7Days(int partnerID,[FromQuery] int chargingStationID)
        {
            return (await _repository.GetPartnerChargingStationRevenueLast7Days(partnerID,chargingStationID)).ToList();
        }

        [HttpGet("GetPartnerChargingStationRevenueLast30Days/{partnerID}")]
        public async Task<ActionResult<List<double>>> GetPartnerChargingStationRevenueLast30Days(int partnerID,[FromQuery] int chargingStationID)
        {
            return (await _repository.GetPartnerChargingStationRevenueLast30Days(partnerID,chargingStationID)).ToList();
        }

        [HttpGet("GetPartnerChargingStationRevenueLast12Months/{partnerID}")]
        public async Task<ActionResult<List<double>>> GetPartnerChargingStationRevenueLast12Months(int partnerID,[FromQuery] int chargingStationID)
        {
            return (await _repository.GetPartnerChargingStationRevenueLast12Months(partnerID,chargingStationID)).ToList();
        }


        [HttpGet("GetPartnerTop10ChargingStationsByRevenue/{partnerID}")]
        public async Task<IActionResult> GetPartnerTop10ChargingStationsByRevenue(int partnerID)
        {
            return Ok(await _repository.GetPartnerTop10ChargingStationsByRevenue(partnerID));
        }





    }
}