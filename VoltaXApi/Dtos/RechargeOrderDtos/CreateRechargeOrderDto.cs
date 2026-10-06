using System.ComponentModel.DataAnnotations;
using VoltaXApi.Models;

namespace VoltaXApi.Dtos
{
    public class CreateRechargeOrderDto
    {
        [Range(1, int.MaxValue)]
        public int CardID { get; set; }
        [EnumDataType(typeof(RechargeOrderStatus))]
        public RechargeOrderStatus Status { get; set; }
        [Range(0.01, 100000)]
        public double Amount { get; set; }
        public DateTime? RechargeDate { get; set; } = DateTime.UtcNow;
    }
}