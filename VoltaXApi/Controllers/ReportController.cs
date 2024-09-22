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
    public class ReportController : GenericController<Report>
    {
        private readonly IReportRepository _repository;
        private readonly IReportService _ReportService;

        public ReportController(IReportRepository repository) : base(repository)
        {
            _repository = repository;
        }

        [HttpPost("CreateReport")]
        public async Task<IActionResult> CreateReport(CreateReportDto reportDto)
        {
            await this._repository.CreateReport(reportDto);
            return StatusCode(204);
        }

        [HttpGet("GetReportByID/{reportID}")]
        public async Task<IActionResult> GetReportByID(int reportID)
        {
            var report = await this._repository.GetReportByID(reportID);
            return Ok(report);
        }
    }
}