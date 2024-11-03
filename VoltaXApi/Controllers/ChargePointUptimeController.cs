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

namespace VoltaXApi.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class ChargePointUptimeController : GenericController<ChargePointUptime>
    {
        private readonly IChargePointUptimeRepository _repository;
        private readonly IMapper _mapper;

        public ChargePointUptimeController(
          IChargePointUptimeRepository repository,
          IMapper mapper
          ) : base(repository)
        {
            _repository = repository;
            _mapper = mapper;
        }

        [HttpGet("GetChargePointUptime/{chargePointID}")]
        public async Task<IActionResult> GetChargePointUptimes(int chargePointID,[FromQuery] GlobalParams globalParams)
        {
            var chargePointUptimes = await PagedList<ChargePointUptime>.CreateAsync((await _repository.GetAllAsync(globalParams)).Where(u => u.ChargePointID == chargePointID), globalParams.PageNumber, globalParams.PageSize);
            Response.AddPagination(chargePointUptimes.CurrentPage, chargePointUptimes.PageSize, chargePointUptimes.TotalCount, chargePointUptimes.TotalPages);
            List<ChargePointUptimeListDto> chargePointUptimesDto = _mapper.Map<List<ChargePointUptimeListDto>>(chargePointUptimes);
            return Ok(chargePointUptimesDto);
        }

    }
}