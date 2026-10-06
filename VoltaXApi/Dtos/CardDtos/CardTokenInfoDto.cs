using VoltaXApi.Models;

namespace VoltaXApi.Dtos
{
    public class CardTokenInfoDto
    {
        public int ID { get; set; }
        public string CardNumber { get; set; }
        public CardTypeEnum CardType { get; set; }
        public double Balance { get; set; }
    }
}
