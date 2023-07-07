namespace VoltaXApi.Dtos
{
    public class RechargeOrderDto
    {
        public int CardID { get; set; }
        public double RechargeAmount { get; set; }
        public string? CardNumber { get; set; }
        public string? CardHolderName { get; set; }
        public string? CardExpirationDate { get; set; }
        public string? CardCVV { get; set; }
        public int? DebitCardID { get; set; }
        public int UserID { get; set; }
        public bool SaveCard { get; set; }
    }
}