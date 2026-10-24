using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using VoltaXApi.Data;
using VoltaXApi.Models;

namespace VoltaXApi.Controllers
{
    /// <summary>
    /// Saved payment cards, as administered from the dashboard. Customers add cards on the payment provider's
    /// page (api/payments/methods/setup) and manage them through api/payments/methods.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class DebitCardController : GenericController<DebitCard>
    {
        public DebitCardController(IRepository<DebitCard> repository) : base(repository)
        {
        }

        // Cards are only ever added by the payment provider and never edited in place.
        [NonAction]
        public override Task<IActionResult> Create(DebitCard entity) => Task.FromResult<IActionResult>(BadRequest());

        [NonAction]
        public override Task<IActionResult> Update(int id, JsonElement entityToUpdate) => Task.FromResult<IActionResult>(BadRequest());
    }
}
