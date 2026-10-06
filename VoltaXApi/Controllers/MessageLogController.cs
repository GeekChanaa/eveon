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
using AutoMapper.QueryableExtensions;

namespace VoltaXApi.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class MessageLogController : GenericController<MessageLog>
    {
        private readonly IMessageLogRepository _repository;
        private readonly IMapper _mapper;

        public MessageLogController(
            IMessageLogRepository repository,
            IMapper mapper) : base(repository)
        {
            _repository = repository;
            _mapper = mapper;
        }


        [HttpGet("GetChargePointMessageLogs/{chargePointID}")]
        public async Task<ActionResult<List<MessageLog>>> GetPartnerMessageLogs(
            string chargePointId,
            [FromQuery] GlobalParams globalParams,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null)
        {
            var msgLogs = _repository.GetAllAsync(globalParams).Where(u => u.ChargePointId == chargePointId);

            // LogTime is stored in UTC; the client sends ISO strings with an offset, which bind as UTC.
            if (from.HasValue)
            {
                var fromUtc = from.Value.ToUniversalTime();
                msgLogs = msgLogs.Where(u => u.LogTime >= fromUtc);
            }
            if (to.HasValue)
            {
                var toUtc = to.Value.ToUniversalTime();
                msgLogs = msgLogs.Where(u => u.LogTime <= toUtc);
            }

            var msgLogsDto = msgLogs.ProjectTo<MessageLogListDto>(_mapper.ConfigurationProvider);
            var MessageLogs = await PagedList<MessageLogListDto>.CreateAsync(msgLogsDto, globalParams.PageNumber, globalParams.PageSize);
            Response.AddPagination(MessageLogs.CurrentPage, MessageLogs.PageSize, MessageLogs.TotalCount, MessageLogs.TotalPages);
            return Ok(MessageLogs);
        }

        
    }
}