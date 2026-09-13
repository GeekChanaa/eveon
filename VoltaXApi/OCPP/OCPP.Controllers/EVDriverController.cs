using Microsoft.AspNetCore.Mvc;
using VoltaXApi.Data;
using Microsoft.AspNetCore.SignalR;
using VoltaXApi.Models;
using VoltaXApi.Dtos;
using System.Threading.Tasks;
using System.Security.Claims;
using VoltaXApi.OCPP.Services;
using VoltaXApi.OCPP.Messages;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Authorization;
using System.Linq;

namespace VoltaXApi.OCPP.Controllers
{
    [ApiController]
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
            Console.WriteLine("Received RequestStartTransaction for ChargePointID: {0}", chargePointID);
            var obj = JsonConvert.SerializeObject(request);
            await _EVDriverService.RequestStartTransaction(chargePointID, request);
            return Ok(new { Message = "Request to start transaction sent successfully." });
        }

        [Authorize]
        [HttpPost("RequestStartTransactionMobile/{chargePointID}")]
        public async Task<IActionResult> RequestStartTransactionMobile(string chargePointID, RequestStartTransactionRequest request)
        {
            var userIdClaim = Request.HttpContext.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return Unauthorized(new { Message = "Invalid or missing user authentication." });
            }

            Console.WriteLine("Received RequestStartTransactionMobile for ChargePointID: {0}, UserID: {1}", chargePointID, userId);
            var obj = JsonConvert.SerializeObject(request);
            await _EVDriverService.RequestStartTransactionMobile(chargePointID, request, userId);
            return Ok(new { Message = "Request to start transaction sent successfully." });
        }

        [HttpPost("RequestStopTransaction/{chargePointID}")]
        public async Task<IActionResult> RequestStopTransaction(string chargePointID, RequestStopTransactionRequest request)
        {
            await _EVDriverService.RequestStopTransaction(chargePointID, request);
            return Ok(new { Message = "Request to stop transaction sent successfully." });
        }

        [Authorize]
        [HttpPost("RequestStopTransactionMobile/{chargePointID}")]
        public async Task<IActionResult> RequestStopTransactionMobile(string chargePointID, RequestStopTransactionRequest request)
        {
            await _EVDriverService.RequestStopTransactionMobile(chargePointID, request);
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
