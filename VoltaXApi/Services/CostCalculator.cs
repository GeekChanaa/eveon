using VoltaXApi.Models;

namespace VoltaXApi.Services
{
    /// <summary>
    /// Connector pricing, shared by billing (TransactionService, CardService) and the CostUpdated sender so the
    /// amount shown on a charger is the amount the driver is charged. Pure. Energy (kWh) and durations (minutes)
    /// are measurements and stay double; prices and amounts are decimal.
    /// </summary>
    public interface ICostCalculator
    {
        /// <summary>Running cost of an ongoing session: the higher of the energy-based and the time-based price.</summary>
        decimal RunningCost(Connector connector, double chargedKwh, double elapsedMinutes);
        /// <summary>Amount debited from the card when the session ends (time-based).</summary>
        decimal FinalCost(Connector connector, double chargedMinutes);
        decimal EnergyCost(Connector connector, double chargedKwh);
        decimal IdleCost(Connector connector, double idleMinutes);
    }

    public sealed class CostCalculator : ICostCalculator
    {
        /// <summary>For code that is not resolved from the container (the calculator is stateless).</summary>
        public static readonly CostCalculator Instance = new();

        public decimal RunningCost(Connector connector, double chargedKwh, double elapsedMinutes) =>
            Math.Max(EnergyCost(connector, chargedKwh), FinalCost(connector, elapsedMinutes));

        public decimal FinalCost(Connector connector, double chargedMinutes) => (decimal)chargedMinutes * connector.PricePerMinute;

        public decimal EnergyCost(Connector connector, double chargedKwh) => (decimal)chargedKwh * connector.PricePerKWh;

        public decimal IdleCost(Connector connector, double idleMinutes) => (decimal)idleMinutes * connector.PricePerIdleMinute;
    }
}
