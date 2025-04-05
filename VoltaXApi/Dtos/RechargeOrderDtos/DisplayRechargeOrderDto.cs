

using VoltaXApi.Models;

namespace VoltaXApi.Dtos;

public class DisplayRechargeOrderDto
{
        public int ID { get; set; }
        public int CardID { get; set; }
        public string CardNumber { get; set; }
        public double Amount { get; set; }
        public DateTime? RechargeDate { get; set; }
        public string UserName { get; set; }
        public RechargeOrderStatus Status { get; set; }
}