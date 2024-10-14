using Microsoft.AspNetCore.Mvc;
using VoltaXApi.Data;
using Microsoft.AspNetCore.SignalR;
using VoltaXApi.Models;
using VoltaXApi.Dtos;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;
using System;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Net.Http;
using System.Net;
using VoltaXApi.Services;
using VoltaXApi.OCPP.Services;
using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.OCPP.Controllers
{
    [Route("ocpp/[controller]")]
    public class EVDriverController : Controller
    {
        private readonly IEVDriverService _EVDriverService;

        public EVDriverController(
            IEVDriverService EVDriverService
        ){
            _EVDriverService = EVDriverService;
        }

        [HttpPost("RequestStartTransaction/{chargePointID}")]
        public async Task<IActionResult> RequestStartTransaction(string chargePointID, RequestStartTransactionRequest request)
        {
            await _EVDriverService.RequestStartTransaction(chargePointID, request);
            return Ok(new { Message = "Request to start transaction sent successfully." });
        }

        [HttpPost("RequestStopTransaction/{chargePointID}")]
        public async Task<IActionResult> RequestStopTransaction(string chargePointID, RequestStopTransactionRequest request)
        {
            await _EVDriverService.RequestStopTransaction(chargePointID, request);
            return Ok(new { Message = "Request to stop transaction sent successfully." });
        }

        [HttpPost("CancelReservation/{chargePointID}")]
        public async Task<IActionResult> CancelReservation(string chargePointID, CancelReservationRequest request)
        {
            await _EVDriverService.CancelReservation(chargePointID, request);
            return Ok(new { Message = "Reservation cancellation request sent successfully." });
        }

        [HttpPost("ReserveNow/{chargePointID}")]
        public async Task<IActionResult> ReserveNow(string chargePointID, ReserveNowRequest request)
        {
            await _EVDriverService.ReserveNow(chargePointID, request);
            return Ok(new { Message = "Reservation request sent successfully." });
        }

        [HttpPost("UnlockConnector/{chargePointID}")]
        public async Task<IActionResult> UnlockConnector(string chargePointID, UnlockConnectorRequest request)
        {
            await _EVDriverService.UnlockConnector(chargePointID, request);
            return Ok(new { Message = "Unlock connector request sent successfully." });
        }

        [HttpPost("ClearCache/{chargePointID}")]
        public async Task<IActionResult> ClearCache(string chargePointID, ClearCacheRequest request)
        {
            await _EVDriverService.ClearCache(chargePointID, request);
            return Ok(new { Message = "Clear cache request sent successfully." });
        }

        [HttpPost("SendLocalList/{chargePointID}")]
        public async Task<IActionResult> SendLocalList(string chargePointID, SendLocalListRequest request)
        {
            await _EVDriverService.SendLocalList(chargePointID, request);
            return Ok(new { Message = "Send local list request sent successfully." });
        }

        [HttpPost("GetLocalListVersion/{chargePointID}")]
        public async Task<IActionResult> GetLocalListVersion(string chargePointID, GetLocalListVersionRequest request)
        {
            await _EVDriverService.GetLocalListVersion(chargePointID, request);
            return Ok(new { Message = "Get local list version request sent successfully." });
        }


    }
}
