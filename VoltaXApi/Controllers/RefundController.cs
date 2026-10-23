using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using VoltaXApi.Models.Billing;
using VoltaXApi.Services.Billing;

namespace VoltaXApi.Controllers
{
    /// <summary>
    /// Operator refunds of top-ups and charging sessions: what is left to refund (permission ViewRechargeOrders) and
    /// the refund itself (EditRechargeOrders, the permission that credits wallets). Send a new idempotency key per
    /// refund the operator confirms; resending the same request answers the same refund.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public sealed class RefundController : ControllerBase
    {
        private readonly RefundService _refunds;

        public RefundController(RefundService refunds)
        {
            _refunds = refunds;
        }

        [HttpGet("Order/{orderID:int}")]
        public async Task<IActionResult> GetOrderRefunds(int orderID, CancellationToken ct) =>
            await _refunds.OrderSummaryAsync(orderID, ct) is { } summary ? Ok(summary) : NotFound();

        [HttpPost("Order/{orderID:int}")]
        public Task<IActionResult> RefundOrder(int orderID, [FromBody] RefundRequest request, CancellationToken ct) =>
            Run(operatorId => _refunds.RefundOrderAsync(orderID, request, operatorId, ct));

        [HttpGet("Session/{chargingSessionID:int}")]
        public async Task<IActionResult> GetSessionRefunds(int chargingSessionID, CancellationToken ct) =>
            await _refunds.SessionSummaryAsync(chargingSessionID, ct) is { } summary ? Ok(summary) : NotFound();

        [HttpPost("Session/{chargingSessionID:int}")]
        public Task<IActionResult> RefundSession(int chargingSessionID, [FromBody] RefundRequest request, CancellationToken ct) =>
            Run(operatorId => _refunds.RefundSessionAsync(chargingSessionID, request, operatorId, ct));

        [HttpGet("Search")]
        public async Task<IActionResult> Search([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] RefundStatus? status,
            [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default) =>
            Ok(await _refunds.SearchAsync(from, to, status, page, pageSize, ct));

        private async Task<IActionResult> Run(Func<int, Task<RefundDto>> refund)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var operatorId))
                return Unauthorized();
            try
            {
                var result = await refund(operatorId);
                return result.Status == RefundStatus.Failed
                    ? StatusCode(StatusCodes.Status502BadGateway, new { error = result.FailureReason, code = "provider_failed", refund = result })
                    : Ok(result);
            }
            catch (RefundRejectedException ex)
            {
                return StatusCode(ex.Status, new { error = ex.Message, code = ex.Code });
            }
        }
    }
}
