using VoltaXApi.Models;

namespace VoltaXApi.Services
{
    /// <summary>
    /// Prices of one charging session (VAT included, as every EVEON price): taken from the connector when the
    /// session starts and stored on the session, so a later price change never alters a session already started.
    /// </summary>
    public sealed record Tariff(decimal PricePerKWh, decimal PricePerMinute, decimal PricePerIdleMinute, decimal FlatFee, decimal VatRate)
    {
        public static Tariff Of(Connector connector, decimal vatRate) =>
            new(connector.PricePerKWh, connector.PricePerMinute, connector.PricePerIdleMinute, connector.FlatFee, vatRate);

        /// <summary>
        /// The snapshot of <paramref name="session"/>. Sessions started before energy and flat prices were part of
        /// the snapshot take the missing prices from <paramref name="connector"/>.
        /// </summary>
        public static Tariff Of(ChargingSession session, Connector? connector, decimal vatRate) => new(
            session.PricePerKWh ?? connector?.PricePerKWh ?? 0,
            session.PricePerMinute ?? connector?.PricePerMinute ?? 0,
            session.PricePerIdleMinute ?? connector?.PricePerIdleMinute ?? 0,
            session.FlatFee ?? connector?.FlatFee ?? 0,
            session.VatRate ?? vatRate);

        /// <summary>Copies the tariff onto a session that is starting.</summary>
        public void ApplyTo(ChargingSession session)
        {
            session.PricePerKWh = PricePerKWh;
            session.PricePerMinute = PricePerMinute;
            session.PricePerIdleMinute = PricePerIdleMinute;
            session.FlatFee = FlatFee;
            session.VatRate = VatRate;
        }
    }

    /// <summary>The amounts of a session, VAT included, each rounded to the cent.</summary>
    public sealed record SessionCharge(decimal Energy, decimal Time, decimal FlatFee)
    {
        public static readonly SessionCharge None = new(0, 0, 0);

        /// <summary>Energy + time + flat fee: what is debited when the transaction ends (idle time comes later).</summary>
        public decimal Total => Energy + Time + FlatFee;
    }

    /// <summary>
    /// The single billing formula, used for the debit, the live cost (SignalR, low-balance stop, CostUpdated on
    /// the charger screen), invoices and statistics, so the amount shown is the amount charged. Pure.
    ///
    /// A session costs flat fee + kWh x price per kWh + charging minutes x price per minute, plus idle minutes x
    /// price per idle minute once the car stays plugged in longer than the grace period after charging (the whole
    /// idle time is then billed). The flat fee only applies when energy was delivered. Each component is rounded
    /// to the cent, half away from zero; the total is the sum of the rounded components.
    /// </summary>
    public interface ICostCalculator
    {
        SessionCharge Charge(Tariff tariff, double chargedKwh, double chargingMinutes);
        decimal IdleFee(Tariff tariff, double idleMinutes, double gracePeriodSeconds);
        /// <summary>Cost so far of an ongoing session (same formula as the final debit).</summary>
        decimal RunningCost(Tariff tariff, double chargedKwh, double elapsedMinutes);
    }

    public sealed class CostCalculator : ICostCalculator
    {
        /// <summary>For code that is not resolved from the container (the calculator is stateless).</summary>
        public static readonly CostCalculator Instance = new();

        public SessionCharge Charge(Tariff tariff, double chargedKwh, double chargingMinutes)
        {
            var kwh = Math.Max(0, chargedKwh);
            var minutes = Math.Max(0, chargingMinutes);
            return new SessionCharge(
                Energy: Round((decimal)kwh * tariff.PricePerKWh),
                Time: Round((decimal)minutes * tariff.PricePerMinute),
                FlatFee: kwh > 0 ? Round(tariff.FlatFee) : 0);
        }

        public decimal IdleFee(Tariff tariff, double idleMinutes, double gracePeriodSeconds) =>
            idleMinutes * 60 <= gracePeriodSeconds ? 0 : Round((decimal)idleMinutes * tariff.PricePerIdleMinute);

        public decimal RunningCost(Tariff tariff, double chargedKwh, double elapsedMinutes) =>
            Charge(tariff, chargedKwh, elapsedMinutes).Total;

        public static decimal Round(decimal amount) => Math.Round(amount, 2, MidpointRounding.AwayFromZero);

        /// <summary>Splits a VAT-included amount: excluding VAT rounded to the cent, VAT = the remainder.</summary>
        public static (decimal ExclVat, decimal Vat) SplitVat(decimal inclVat, decimal vatRate)
        {
            var excl = Round(inclVat / (1 + vatRate));
            return (excl, inclVat - excl);
        }
    }
}
