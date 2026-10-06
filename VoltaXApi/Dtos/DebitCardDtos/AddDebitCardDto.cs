using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using VoltaXApi.Models;

namespace VoltaXApi.Dtos
{
    /// <summary>
    /// POST api/DebitCard. Only a payment-provider token and display metadata are accepted.
    /// Unknown fields (cardNumber, cvv, ...) fail model binding, and any value that looks like a
    /// full card number is rejected, so a PAN can never reach the database or the logs.
    /// </summary>
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public class AddDebitCardDto : IValidatableObject
    {
        private static readonly Regex LooksLikePan = new(@"(?:\d[ -]?){12,19}", RegexOptions.Compiled);

        [Required, StringLength(255, MinimumLength = 8)]
        [RegularExpression(@"^[A-Za-z0-9_\-]+$", ErrorMessage = "Invalid card token.")]
        public string ProviderToken { get; set; } = string.Empty;

        [EnumDataType(typeof(DebitCardTypeEnum))]
        public DebitCardTypeEnum Brand { get; set; } = DebitCardTypeEnum.Generic;

        [Required, RegularExpression(@"^\d{4}$", ErrorMessage = "Last4 must be exactly four digits.")]
        public string Last4 { get; set; } = string.Empty;

        [Range(1, 12)]
        public int ExpiryMonth { get; set; }

        [Range(2000, 2100)]
        public int ExpiryYear { get; set; }

        [StringLength(100)]
        public string? Name { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (LooksLikePan.IsMatch(ProviderToken ?? "") || LooksLikePan.IsMatch(Name ?? ""))
                yield return new ValidationResult("Full card numbers are not accepted.", new[] { nameof(ProviderToken) });

            var now = DateTime.UtcNow;
            if (ExpiryYear < now.Year || ExpiryYear == now.Year && ExpiryMonth < now.Month)
                yield return new ValidationResult("The card has expired.", new[] { nameof(ExpiryYear) });
        }
    }
}
