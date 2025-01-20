using VoltaXApi.Models;
using Microsoft.AspNetCore.Mvc;
using VoltaXApi.Data;
using VoltaXApi.Services;
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

namespace VoltaXApi.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class SystemReportCommentController : GenericController<SystemReportComment>
    {

      private readonly ISystemReportCommentRepository _repository;

        public SystemReportCommentController(
          ISystemReportCommentRepository repository
        ) : base(repository)
        {
            _repository = repository;
        }

        [HttpGet("GetSystemReportComments/{systemReportID}")]
        public async Task<IActionResult> GetSystemReportComments(int systemReportID,[FromQuery] GlobalParams globalParams)
        {
            var systemReportComments = await PagedList<SystemReportCommentDisplayDto>.CreateAsync((_repository.GetSystemReportComments(systemReportID)), globalParams.PageNumber, globalParams.PageSize);
            Response.AddPagination(systemReportComments.CurrentPage, systemReportComments.PageSize, systemReportComments.TotalCount, systemReportComments.TotalPages);
            return Ok(systemReportComments);
        }
    }
}