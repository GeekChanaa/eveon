namespace VoltaXApi.Dtos
{
    public class OrderDto
    {
        public int ID { get; set; }
        public int CardID { get; set; }
        public double Amount { get; set; }
        public DateTime RechargeDate { get; set; }
    }
}