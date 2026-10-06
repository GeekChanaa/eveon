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
        private readonly ChargingSessionInvoiceGeneratorService _invoiceGenerator;

        public ChargingSessionController(
            IChargingSessionRepository repository,
            ChargingSessionInvoiceGeneratorService invoiceGeneratorService) : base(repository)
        {
            _repository = repository;
            _invoiceGenerator = invoiceGeneratorService;
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
            return Ok(await _repository.GetChargingSessionInformations(chargingSessionID));
        }

        [HttpGet("GetChargingSessionInvoice/{chargingSessionID}")]
        public async Task<IActionResult> GetChargingSessionInvoice(int chargingSessionID)
        {
            var chargingSession = await _repository.GetChargingSessionInformations(chargingSessionID);
            var chargingSessionInvoice = new ChargingSessionInvoice
            {
                UserName = chargingSession.UserName,
                SessionDate = chargingSession.StartDate,
                StartDate = chargingSession.StartDate,
                EndDate = chargingSession.EndDate,
                ConnectorID = chargingSession.ConnectorID,
                ChargePointName = chargingSession.ChargePointName,
                TotalKwhCharged = chargingSession.KwhCharged ?? 0,
                TotalPrice = chargingSession.TotalPriceWithVAT ?? 0,
                TotalPriceWithoutVAT = chargingSession.TotalPriceWithoutVAT ?? 0,
                TotalPriceWithVAT = chargingSession.TotalPriceWithVAT ?? 0,
                ChargingPriceWithVAT = chargingSession.ChargingPriceWithVAT ?? 0,
                IdlePriceWithVAT = chargingSession.IdldePriceWithVAT ?? 0,
                CardNumber  = chargingSession.CardNumber,
                ChargedMinutes = chargingSession.ChargedMinutes ?? 0,
                IdleMinutes = chargingSession.IdleMinutes ?? 0,
                PricePerIdleMinute = chargingSession.PricePerIdleMinute ?? 0,
                PricePerMinute = chargingSession.PricePerMinute ?? 0,
                KwhsCharged = chargingSession.KwhCharged ?? 0,
                CardBalance = chargingSession.CardBalance,
                ChargingSessionID = chargingSessionID
            };
            var pdfBytes = _invoiceGenerator.GenerateInvoice(chargingSessionInvoice);

            return File(pdfBytes, "application/pdf", "Invoice.pdf");
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
