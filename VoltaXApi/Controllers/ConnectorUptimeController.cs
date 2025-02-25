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
    public class ConnectorUptimeController : GenericController<ConnectorUptime>
    {
        private readonly IConnectorUptimeRepository _repository;
        private readonly IMapper _mapper;

        public ConnectorUptimeController(
          IConnectorUptimeRepository repository,
          IMapper mapper
          ) : base(repository)
        {
            _repository = repository;
            _mapper = mapper;
        }

        [HttpGet("GetConnectorUptime/{connectorID}")]
        public async Task<IActionResult> GetConnectorUptimes(int connectorID,[FromQuery] GlobalParams globalParams)
        {
            var connectorUptimes = await PagedList<ConnectorUptime>.CreateAsync(_repository.GetAllAsync(globalParams).Where(u => u.ConnectorID == connectorID), globalParams.PageNumber, globalParams.PageSize);
            Response.AddPagination(connectorUptimes.CurrentPage, connectorUptimes.PageSize, connectorUptimes.TotalCount, connectorUptimes.TotalPages);
            List<ConnectorUptimeListDto> connectorUptimesDto = _mapper.Map<List<ConnectorUptimeListDto>>(connectorUptimes);
            return Ok(connectorUptimesDto);
        }

    }
}