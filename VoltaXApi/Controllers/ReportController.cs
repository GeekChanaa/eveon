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
using VoltaxApi.Dtos;

namespace VoltaXApi.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class ReportController : GenericController<Report>
    {
        private readonly IReportRepository _repository;
        private readonly IReportService _reportService;

        public ReportController(
            IReportRepository repository,
            IReportService reportService) : base(repository)
        {
            _repository = repository;
            _reportService = reportService;
        }

        [HttpPost("CreateReport")]
        public async Task<IActionResult> CreateReport(CreateReportDto reportDto)
        {
            await this._reportService.HandleReport(reportDto);
            return StatusCode(204);
        }

        [HttpGet("GetReportByID/{reportID}")]
        public async Task<IActionResult> GetReportByID(int reportID)
        {
            var report = await this._repository.GetReportByID(reportID);
            return Ok(report);
        }

        [HttpGet("GetAllReports")]
        public async Task<PagedList<ReportListDto>> GetAllReports([FromQuery] GlobalParams globalParams)
        {
            var reports = this._repository.GetAllReports(globalParams);
            var reportsList = await PagedList<ReportListDto>.CreateAsync(reports,globalParams.PageNumber, globalParams.PageSize);
            Response.AddPagination(reportsList.CurrentPage, reportsList.PageSize, reportsList.TotalCount, reportsList.TotalPages);
            return reportsList;
        }
    }
}