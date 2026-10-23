

using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
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
    public double? ChargedKwhs { get; set; }
    [Precision(18, 4)]
    public decimal? PricePerMinute { get; set; }
    [Precision(18, 4)]
    public decimal? CostPerKwh { get; set; }
    [Precision(18, 4)]
    public decimal? PricePerIdleMinute { get; set; }
    public ReasonEnumType StoppedReason { get; set; }
    public Connector? Connector { get; set; }
      public ChargingSessionStatusEnum ChargingSessionStatus { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public User? User { get; set; }
    public Card? Card { get; set; }
    public ICollection<Transaction>? Transactions { get; set; }

    public decimal ChargingPriceWithVAT { get { return Math.Round((decimal)(ChargedMinutes ?? 0) * (PricePerMinute ?? 0), 2); } }
    public decimal? PricePerHour { get { return PricePerMinute == null ? 0 : PricePerMinute * 60; } }

    public decimal ChargingPriceWithoutVAT(decimal vatRate)
        => Math.Round(ChargingPriceWithVAT / (1 + vatRate), 2);

    public decimal VATChargingPrice(decimal vatRate)
        => Math.Round(ChargingPriceWithVAT * vatRate, 2);

    public decimal IdleChargingPriceWithVAT(int gracePeriodSeconds)
    {
      double idleMinutes = IdleMinutes ?? 0;
      double graceMinutes = (double) (gracePeriodSeconds / 60.0);

      if (idleMinutes <= graceMinutes) return 0; // Grace period check

      return Math.Round((decimal)idleMinutes * (PricePerIdleMinute ?? 0), 2);
    }

    public decimal IdleChargingPriceWithoutVAT(decimal vatRate, int gracePeriodSeconds)
        => Math.Round(IdleChargingPriceWithVAT(gracePeriodSeconds) / (1 + vatRate), 2);

    public decimal VATIdleChargingPrice(decimal vatRate, int gracePeriodSeconds)
        => Math.Round(IdleChargingPriceWithVAT(gracePeriodSeconds) * vatRate, 2);

    public decimal TotalPriceWithoutVAT(decimal vatRate, int gracePeriodSeconds)
        => ChargingPriceWithoutVAT(vatRate) + IdleChargingPriceWithoutVAT(vatRate, gracePeriodSeconds);

    public decimal TotalPriceWithVAT(decimal vatRate, int gracePeriodSeconds)
        => ChargingPriceWithVAT + IdleChargingPriceWithVAT(gracePeriodSeconds);
  }
}