using System.ComponentModel.DataAnnotations;
using VoltaXApi.Models;

namespace VoltaXApi.Dtos
{
    public class CreateCardDto
    {
        public int ID { get; set; }
        [StringLength(32)]
        [RegularExpression(@"^[0-9A-Za-z\-]*$")]
        public string? CardNumber { get; set; }
        [EnumDataType(typeof(CardTypeEnum))]
        public CardTypeEnum CardType { get; set; }
        public DateTime ExpirationDate { get; set; } = new DateTime().AddYears(2);
        [Range(0, int.MaxValue)]
        public int MaxCount { get; set; } = 3000;
        [EnumDataType(typeof(CardStatusEnum))]
        public CardStatusEnum Status { get; set; }
        [Range(0, 1000000)]
        public double Balance { get; set; }
        [StringLength(500)]
        public string Note { get; set; }
        public int? UserID { get; set; }
    }
}