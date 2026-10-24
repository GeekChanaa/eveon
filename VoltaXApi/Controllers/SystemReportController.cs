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
using VoltaXApi.Services;

namespace VoltaXApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SystemReportController : GenericController<SystemReport>
    {
        private readonly ISystemReportRepository _repository;
        private readonly ISystemReportService _systemReportService;

        public SystemReportController(
            ISystemReportRepository repository,
            ISystemReportService systemReportService) : base(repository)
        {
            _repository = repository;
            _systemReportService = systemReportService;
        }

        [HttpPost("CreateSystemReport")]
        public async Task<IActionResult> CreateReport(SystemReport report)
        {
            // Only the plain fields: a bound entity would also insert nested objects (user, card, charge point...).
            await this._systemReportService.HandleReport(VoltaXApi.Helpers.EntityUpdate.NewFromScalars(report));
            return StatusCode(204);
        }

        [HttpGet("GetSystemReport/{systemReportID}")]
        public async Task<IActionResult> GetSystemReport(int systemReportID)
        {
            return Ok(await this._repository.GetSystemReport(systemReportID));
        }
    }
}
