using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using VoltaXApi.Models;
using VoltaXApi.Services.Gdpr;

namespace VoltaXApi.Controllers
{
    // Mapped to the "Users" resource: listing needs ViewUsers, cancelling needs EditUsers.
    [Route("api/account-deletions")]
    [ApiController]
    public class AccountDeletionController : ControllerBase
    {
        private readonly IAccountDeletionService _service;

        public AccountDeletionController(IAccountDeletionService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<ActionResult<List<PendingAccountDeletionDto>>> GetAccountDeletions([FromQuery] AccountDeletionStatusEnum status = AccountDeletionStatusEnum.Pending)
            => await _service.ListAsync(status);

        // Login is disabled during the grace period, so a user who changes their mind goes through support.
        [HttpPost("{userID}/cancel")]
        public async Task<IActionResult> CancelAccountDeletion(int userID)
        {
            try
            {
                int? adminId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
                await _service.CancelAsync(userID, adminId);
                return NoContent();
            }
            catch (AccountDeletionException)
            {
                return NotFound("No pending deletion for this account.");
            }
        }
    }
}
