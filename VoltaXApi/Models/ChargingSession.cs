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

    // Tariff snapshot taken when the session starts (prices include VAT).
    [Precision(18, 4)]
    public decimal? PricePerKWh { get; set; }
    [Precision(18, 4)]
    public decimal? PricePerMinute { get; set; }
    [Precision(18, 4)]
    public decimal? PricePerIdleMinute { get; set; }
    [Precision(18, 4)]
    public decimal? FlatFee { get; set; }
    [Precision(18, 4)]
    public decimal? CostPerKwh { get; set; }
    [Precision(5, 4)]
    public decimal? VatRate { get; set; }

    // Amounts billed (VAT included), set when the transaction ends; IdleAmount when the car leaves.
    [Precision(18, 2)]
    public decimal? EnergyAmount { get; set; }
    [Precision(18, 2)]
    public decimal? TimeAmount { get; set; }
    [Precision(18, 2)]
    public decimal? FlatFeeAmount { get; set; }
    [Precision(18, 2)]
    public decimal? IdleAmount { get; set; }
    [Precision(18, 2)]
    public decimal? TotalAmount { get; set; }
    // Card balance around the session debit, for the invoice.
    [Precision(18, 2)]
    public decimal? BalanceBefore { get; set; }
    [Precision(18, 2)]
    public decimal? BalanceAfter { get; set; }
    public DateTime? BilledAt { get; set; }

    public ReasonEnumType StoppedReason { get; set; }
    public Connector? Connector { get; set; }
      public ChargingSessionStatusEnum ChargingSessionStatus { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public User? User { get; set; }
    public Card? Card { get; set; }
    public ICollection<Transaction>? Transactions { get; set; }

    /// <summary>True once the transaction ended and the session was billed with the current formula.</summary>
    [NotMapped]
    public bool IsBilled => TotalAmount.HasValue;

    /// <summary>
    /// Energy + time + flat fee, VAT included. Sessions billed before the amounts were stored were billed on time
    /// only, which is what they show.
    /// </summary>
    public decimal ChargingPriceWithVAT => IsBilled
        ? (EnergyAmount ?? 0) + (TimeAmount ?? 0) + (FlatFeeAmount ?? 0)
        : Math.Round((decimal)(ChargedMinutes ?? 0) * (PricePerMinute ?? 0), 2);
    public decimal? PricePerHour { get { return PricePerMinute == null ? 0 : PricePerMinute * 60; } }

    public decimal ChargingPriceWithoutVAT(decimal vatRate)
        => Math.Round(ChargingPriceWithVAT / (1 + (VatRate ?? vatRate)), 2, MidpointRounding.AwayFromZero);

    /// <summary>VAT contained in the (VAT included) charging price.</summary>
    public decimal VATChargingPrice(decimal vatRate)
        => ChargingPriceWithVAT - ChargingPriceWithoutVAT(vatRate);

    public decimal IdleChargingPriceWithVAT(int gracePeriodSeconds)
    {
      if (IdleAmount.HasValue || IsBilled)
        return IdleAmount ?? 0;

      double idleMinutes = IdleMinutes ?? 0;
      double graceMinutes = (double) (gracePeriodSeconds / 60.0);

      if (idleMinutes <= graceMinutes) return 0; // Grace period check

      return Math.Round((decimal)idleMinutes * (PricePerIdleMinute ?? 0), 2);
    }

    public decimal IdleChargingPriceWithoutVAT(decimal vatRate, int gracePeriodSeconds)
        => Math.Round(IdleChargingPriceWithVAT(gracePeriodSeconds) / (1 + (VatRate ?? vatRate)), 2, MidpointRounding.AwayFromZero);

    public decimal VATIdleChargingPrice(decimal vatRate, int gracePeriodSeconds)
        => IdleChargingPriceWithVAT(gracePeriodSeconds) - IdleChargingPriceWithoutVAT(vatRate, gracePeriodSeconds);

    public decimal TotalPriceWithoutVAT(decimal vatRate, int gracePeriodSeconds)
        => ChargingPriceWithoutVAT(vatRate) + IdleChargingPriceWithoutVAT(vatRate, gracePeriodSeconds);

    public decimal TotalPriceWithVAT(decimal vatRate, int gracePeriodSeconds)
        => ChargingPriceWithVAT + IdleChargingPriceWithVAT(gracePeriodSeconds);
  }
}
