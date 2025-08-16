

using System.ComponentModel.DataAnnotations.Schema;
using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.Models
{
  public class ChargingSession : IEntity
  {
    public int ID { get; set; }
    public int ConnectorID { get; set; }
    public int UserID { get; set; }
    public int CardID { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? EndIdleDate { get; set; }
    public double? ChargedMinutes { get; set; }
    public double? IdleMinutes { get; set; }
    public double? PricePerMinute { get; set; }
    public double? PricePerIdleMinute { get; set; }
    public ReasonEnumType StoppedReason { get; set; }
    public Connector? Connector { get; set; }
      public ChargingSessionStatusEnum ChargingSessionStatus { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public User? User { get; set; }
    public Card? Card { get; set; }
    public ICollection<Transaction>? Transactions { get; set; }

    public double ChargingPriceWithVAT { get { return Math.Round((ChargedMinutes ?? 0) * (PricePerMinute ?? 0), 2); } }
    public double? PricePerHour { get { return PricePerMinute == null ? 0 : PricePerMinute * 60; } }

    public double ChargingPriceWithoutVAT(double vatRate)
        => Math.Round(ChargingPriceWithVAT / (1 + vatRate), 2);

    public double VATChargingPrice(double vatRate)
        => Math.Round(ChargingPriceWithVAT * vatRate, 2);

    public double IdleChargingPriceWithVAT(int gracePeriodSeconds)
    {
      double idleMinutes = IdleMinutes ?? 0;
      double graceMinutes = (double) (gracePeriodSeconds / 60.0);

      if (idleMinutes <= graceMinutes) return 0; // Grace period check

      return Math.Round(idleMinutes * (PricePerIdleMinute ?? 0), 2);
    }

    public double IdleChargingPriceWithoutVAT(double vatRate, int gracePeriodSeconds)
        => Math.Round(IdleChargingPriceWithVAT(gracePeriodSeconds) / (1 + vatRate), 2);

    public double VATIdleChargingPrice(double vatRate, int gracePeriodSeconds)
        => Math.Round(IdleChargingPriceWithVAT(gracePeriodSeconds) * vatRate, 2);

    public double TotalPriceWithoutVAT(double vatRate, int gracePeriodSeconds)
        => ChargingPriceWithoutVAT(vatRate) + IdleChargingPriceWithoutVAT(vatRate, gracePeriodSeconds);

    public double TotalPriceWithVAT(double vatRate, int gracePeriodSeconds)
        => ChargingPriceWithVAT + IdleChargingPriceWithVAT(gracePeriodSeconds);
  }
}