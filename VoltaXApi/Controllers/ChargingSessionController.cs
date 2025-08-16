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
                ChargePointName = chargingSession.ChargePointName,
                TotalKwhCharged = chargingSession.KwhCharged ?? 0,
                TotalPrice = chargingSession.TotalPriceWithVAT ?? 0,
                Transactions = chargingSession.Transactions
                                            .Select(cs => new TransactionItem
                                            {
                                                StartTime = cs.StartTime,
                                                StopTime = cs.StopTime,
                                                MeterStart = cs.MeterStart,
                                                MeterStop = cs.MeterStop,
                                                Amount = cs.Amount
                                            })
                                            .ToList()
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


    }
}