namespace VoltaXApi.Models
{
    public class Transaction
{
    public int ID { get; set; }


    public int UserID { get; set; }
    public User User { get; set; } // Navigation property

    public int ChargePointID { get; set; }
    public ChargePoint ChargePoint { get; set; } // Navigation property

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public double EnergyConsumed { get; set; }

    public decimal PaymentAmount { get; set; }

    public string TransactionStatus { get; set; }
}

}