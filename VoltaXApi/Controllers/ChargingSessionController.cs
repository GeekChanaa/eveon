using VoltaXApi.Models;
using Microsoft.AspNetCore.Mvc;
using VoltaXApi.Data;
using VoltaXApi.Dtos;
using System.Threading;
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
using VoltaxApi.Dtos;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace VoltaXApi.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class ChargingSessionController : GenericController<ChargingSession>
    {
        private readonly IChargingSessionRepository _repository;

        public ChargingSessionController(IChargingSessionRepository repository) : base(repository)
        {
            _repository = repository;
        }

        [HttpGet("GetChargePointChargingSessions/{chargePointID}")]
        public async Task<PagedList<ChargePointChargingSessionListDto>> GetChargePointChargingSessions(int chargePointID, [FromQuery] GlobalParams globalParams)
        {
            var chargingSessions = this._repository.GetChargePointChargingSessions(chargePointID, globalParams);
            var chargingSessionsList = await PagedList<ChargePointChargingSessionListDto>.CreateAsync(chargingSessions, globalParams.PageNumber, globalParams.PageSize);
            Response.AddPagination(chargingSessionsList.CurrentPage, chargingSessionsList.PageSize, chargingSessionsList.TotalCount, chargingSessionsList.TotalPages);
            return chargingSessionsList;
        }

        [HttpGet("GetChargingSessionInformations/{chargingSessionID}")]
        public async Task<IActionResult> GetChargingSessionInformations(int chargingSessionID)
        {
            var session = await _repository.GetChargingSessionInformations(chargingSessionID);
            return session == null ? NotFound() : Ok(session);
        }

        /// <summary>The legal invoice of an ended, billed session (issued on first request, then always the stored PDF).</summary>
        [HttpGet("GetChargingSessionInvoice/{chargingSessionID}")]
        public async Task<IActionResult> GetChargingSessionInvoice(int chargingSessionID,
            [FromServices] VoltaXApi.Services.Billing.IInvoiceService invoices, CancellationToken ct)
        {
            var invoice = await invoices.IssueForSessionAsync(chargingSessionID, ct);
            var file = invoice == null ? null : await invoices.GetFileAsync(invoice.ID, ct);
            return file == null ? NotFound() : File(file.Content, file.ContentType, file.FileName);
        }

        /// <summary>Every session of the network with filters (period, status, station, charge point, customer, text), newest first.</summary>
        [HttpGet("Search")]
        public async Task<IActionResult> Search([FromQuery] OperatorSessionFilter filter,
            [FromServices] VoltaXApi.Services.Sessions.OperatorSearchService search, CancellationToken ct) =>
            Ok(await search.SessionsAsync(filter, ct));

        /// <summary>The filtered sessions as a CSV file or a printable PDF statement (at most 20,000 sessions).</summary>
        [HttpGet("Export")]
        public async Task<IActionResult> Export([FromQuery] OperatorSessionFilter filter, [FromQuery] string format,
            [FromServices] VoltaXApi.Services.Sessions.OperatorSearchService search,
            [FromServices] VoltaXApi.Services.CustomerPortal.SessionStatementExporter exporter,
            [FromServices] GlobalConfigurations pricing, CancellationToken ct)
        {
            if (format is not ("csv" or "pdf"))
                return BadRequest(new { error = "format must be csv or pdf" });
            var rows = await search.SessionStatementAsync(filter, ct);
            var name = $"eveon-sessions-{DateTime.UtcNow:yyyyMMdd}";
            return format == "csv"
                ? File(exporter.Csv(rows), "text/csv; charset=utf-8", name + ".csv")
                : File(exporter.Pdf("EVEON network", filter.From, filter.To, rows, pricing.Vat), "application/pdf", name + ".pdf");
        }

        [HttpGet("GetChargePointNbrChargingSessions/{chargePointID}")]
        public async Task<IActionResult> GetChargePointNbrChargingSessions(int chargePointID)
        {
            var result = await _repository.GetChargePointNbrChargingSessions(chargePointID);
            return Ok(result);
        }

        [HttpGet("GetChargePointNbrChargingSessionsToday/{chargePointID}")]
        public async Task<IActionResult> GetChargePointNbrChargingSessionsToday(int chargePointID)
        {
            var result = await _repository.GetChargePointNbrChargingSessionsToday(chargePointID);
            return Ok(result);
        }

        [HttpGet("GetChargePointNbrChargingSessionsLast30Days/{chargePointID}")]
        public async Task<IActionResult> GetChargePointNbrChargingSessionsLast30Days(int chargePointID)
        {
            var result = await _repository.GetChargePointNbrChargingSessionsLast30Days(chargePointID);
            return Ok(result);
        }

        [HttpGet("GetChargingSessions")]
        public async Task<List<ChargingSessionListDto>> GetChargingSessions([FromQuery] GlobalParams globalParams)
        {
            var chargingSessions = this._repository.GetChargingSessions();
            var chargingSessionsList = await PagedList<ChargingSessionListDto>.CreateAsync(chargingSessions, globalParams.PageNumber, globalParams.PageSize);
            Response.AddPagination(chargingSessionsList.CurrentPage, chargingSessionsList.PageSize, chargingSessionsList.TotalCount, chargingSessionsList.TotalPages);
            return chargingSessionsList;
        }

        [HttpGet("GetUserChargingSessions/{userID}")]
        public async Task<PagedList<ChargingSessionListDto>> GetUserChargingSessions(int userID, [FromQuery] GlobalParams globalParams)
        {
            var chargingSessions = this._repository.GetUserChargingSessions(userID, globalParams);
            var chargingSessionsList = await PagedList<ChargingSessionListDto>.CreateAsync(chargingSessions, globalParams.PageNumber, globalParams.PageSize);
            Response.AddPagination(chargingSessionsList.CurrentPage, chargingSessionsList.PageSize, chargingSessionsList.TotalCount, chargingSessionsList.TotalPages);
            return chargingSessionsList;
        }

        [Authorize]
        [HttpGet("GetMyCurrentChargingSession/")]
        public async Task<IActionResult> GetMyCurrentChargingSession([FromQuery] GlobalParams globalParams)
        {
            var userIdClaim = Request.HttpContext.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
            var chargingSession = await this._repository.GetUserCurrentChargingSession(int.Parse(userIdClaim), globalParams);
            if(chargingSession == null)
            {
                return NotFound(new { Message = "No active charging session found for the user." });
            }
            return Ok(chargingSession);
        }

        [Authorize]
        [HttpGet("GetMyChargingSessions")]
        public async Task<ActionResult<PagedList<MyChargingSessionDto>>> GetMyChargingSessions(
            [FromQuery] GlobalParams globalParams)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userID))
                return Unauthorized();

            if (globalParams.PageNumber < 1 || globalParams.PageSize < 1 || globalParams.PageSize > 50)
                return BadRequest(new { Message = "PageNumber must be at least 1 and PageSize must be between 1 and 50." });

            var query = _repository.GetMyChargingSessions(userID);
            var sessions = await PagedList<MyChargingSessionDto>.CreateAsync(
                query,
                globalParams.PageNumber,
                globalParams.PageSize);

            Response.AddPagination(
                sessions.CurrentPage,
                sessions.PageSize,
                sessions.TotalCount,
                sessions.TotalPages);

            return Ok(sessions);
        }

    }
}
