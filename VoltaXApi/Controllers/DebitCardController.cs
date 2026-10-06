using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using VoltaXApi.Data;
using VoltaXApi.Dtos;
using VoltaXApi.Models;
using VoltaXApi.Services.Payments;

namespace VoltaXApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DebitCardController : GenericController<DebitCard>
    {
        private readonly IRepository<DebitCard> _repository;
        private readonly IPaymentCardTokenizer _tokenizer;

        public DebitCardController(IRepository<DebitCard> repository, IPaymentCardTokenizer tokenizer) : base(repository)
        {
            _repository = repository;
            _tokenizer = tokenizer;
        }

        // Cards are only ever added through AddCard (provider token) and never edited in place.
        [NonAction]
        public override Task<IActionResult> Create(DebitCard entity) => Task.FromResult<IActionResult>(BadRequest());

        [NonAction]
        public override Task<IActionResult> Update(int id, JsonElement entityToUpdate) => Task.FromResult<IActionResult>(BadRequest());

        /// <summary>Saves a card for the caller from a payment-provider token. Card numbers and CVVs are refused.</summary>
        [HttpPost]
        public async Task<IActionResult> AddCard([FromBody] AddDebitCardDto request, CancellationToken cancellationToken)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userID)) return Unauthorized();

            var claimed = new CardDisplayMetadata(request.Brand, request.Last4, request.ExpiryMonth, request.ExpiryYear);
            var verified = await _tokenizer.VerifyAsync(request.ProviderToken, claimed, cancellationToken);

            var card = new DebitCard
            {
                UserID = userID,
                Name = string.IsNullOrWhiteSpace(request.Name) ? null : request.Name.Trim(),
                Brand = verified.Card.Brand,
                Last4 = verified.Card.Last4,
                ExpiryMonth = verified.Card.ExpiryMonth,
                ExpiryYear = verified.Card.ExpiryYear,
                Provider = verified.Provider,
                ProviderToken = verified.ProviderToken
            };
            await _repository.AddAsync(card);

            return Ok(new DebitCardListingDto
            {
                ID = card.ID,
                UserID = userID,
                Type = card.Brand,
                Last4 = card.Last4,
                ExpiryMonth = card.ExpiryMonth,
                ExpiryYear = card.ExpiryYear,
                CardNumberHidden = "•••• " + card.Last4,
                NameHidden = card.Name ?? string.Empty
            });
        }
    }
}
