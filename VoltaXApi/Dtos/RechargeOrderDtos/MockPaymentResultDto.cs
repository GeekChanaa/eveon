using VoltaXApi.Models;

namespace VoltaXApi.Dtos;

public class MockPaymentResultDto
{
    public int OrderID { get; set; }
    public int CardID { get; set; }
    public decimal Amount { get; set; }
    public RechargeOrderStatus Status { get; set; }
    public bool BalanceCredited { get; set; }
    public string Message { get; set; } = string.Empty;
}
