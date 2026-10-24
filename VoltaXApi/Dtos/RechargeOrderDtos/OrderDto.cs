namespace VoltaXApi.Dtos
{
    public class OrderDto
    {
        public int ID { get; set; }
        public int CardID { get; set; }
        public decimal Amount { get; set; }
        public DateTime RechargeDate { get; set; }
    }
}