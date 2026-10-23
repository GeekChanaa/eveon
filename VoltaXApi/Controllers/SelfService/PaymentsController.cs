using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using VoltaXApi.Authorization;
using VoltaXApi.Services.Payments;

namespace VoltaXApi.Controllers.SelfService
{
    public sealed class StartTopUpRequest
    {
        [Range(0.01, 1_000_000)]
        public decimal Amount { get; set; }
        /// <summary>Card to credit; the caller's first active card when omitted.</summary>
        public int? CardId { get; set; }
    }

    /// <summary>The caller's online top-ups and saved payment cards. Every action is scoped to the caller.</summary>
    [ApiController]
    [Route("api/payments")]
    [SelfService]
    public sealed class PaymentsController : ControllerBase
    {
        private readonly PaymentService _payments;

        public PaymentsController(PaymentService payments)
        {
            _payments = payments;
        }

        /// <summary>What the payment page needs: provider, limits, currency and whether cards can be saved.</summary>
        [HttpGet("config")]
        public IActionResult Config() => Ok(new
        {
            provider = _payments.Gateway.Name,
            enabled = _payments.Gateway is not DisabledPaymentGateway,
            currency = _payments.Options.Currency,
            minTopUp = _payments.Options.MinTopUp,
            maxTopUp = _payments.Options.MaxTopUp,
            supportsSavedCards = _payments.Gateway.SupportsSavedCards
        });

        /// <summary>Starts a top-up: returns how the browser reaches the provider's payment page.</summary>
        [HttpPost("top-ups")]
        public async Task<IActionResult> StartTopUp([FromBody] StartTopUpRequest request, CancellationToken cancellationToken)
        {
            var started = await _payments.StartTopUpAsync(this.User(), request.CardId, request.Amount, cancellationToken);
            return Ok(new { orderId = started.OrderId, checkout = started.Checkout });
        }

        [HttpGet("top-ups/{orderId:int}")]
        public async Task<IActionResult> GetTopUp(int orderId, CancellationToken cancellationToken)
        {
            var status = await _payments.GetTopUpAsync(this.User().ID, orderId, cancellationToken);
            return status == null ? NotFound() : Ok(status);
        }

        [HttpGet("methods")]
        public async Task<IActionResult> Methods(CancellationToken cancellationToken) =>
            Ok(await _payments.ListMethodsAsync(this.User().ID, cancellationToken));

        /// <summary>Starts saving a card on the provider's page.</summary>
        [HttpPost("methods/setup")]
        public async Task<IActionResult> SetupMethod(CancellationToken cancellationToken) =>
            Ok(await _payments.StartCardSetupAsync(this.User(), cancellationToken));

        [HttpPost("methods/{id:int}/default")]
        public async Task<IActionResult> SetDefault(int id, CancellationToken cancellationToken) =>
            await _payments.SetDefaultMethodAsync(this.User().ID, id, cancellationToken) ? NoContent() : NotFound();

        [HttpDelete("methods/{id:int}")]
        public async Task<IActionResult> RemoveMethod(int id, CancellationToken cancellationToken) =>
            await _payments.RemoveMethodAsync(this.User().ID, id, cancellationToken) ? NoContent() : NotFound();
    }
}
