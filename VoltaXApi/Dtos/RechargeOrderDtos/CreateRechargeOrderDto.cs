using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.Dtos
{
    /// <summary>A manual top-up credited by an operator.</summary>
    public class CreateRechargeOrderDto
    {
        [Range(1, int.MaxValue)]
        public int CardID { get; set; }
        [Range(0.01, 100000)]
        public decimal Amount { get; set; }
        /// <summary>Why the card is credited without an online payment (cash, bank transfer, goodwill...).</summary>
        [Required, StringLength(256, MinimumLength = 3)]
        public string Note { get; set; } = "";
    }
}
