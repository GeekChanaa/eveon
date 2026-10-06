using VoltaXApi.Models;

namespace VoltaXApi.Dtos
{
    public class DebitCardListingDto
    {
        public int ID { get; set; }
        public int UserID { get; set; }
        public DebitCardTypeEnum Type { get; set; }
        public string Last4 { get; set; } = string.Empty;
        public int ExpiryMonth { get; set; }
        public int ExpiryYear { get; set; }
        /// <summary>"•••• 1234", for display.</summary>
        public string CardNumberHidden { get; set; } = string.Empty;
        public string NameHidden { get; set; } = string.Empty;
    }
}
