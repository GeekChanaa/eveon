using VoltaXApi.Models;

namespace VoltaXApi.Services
{
    /// <summary>
    /// Connector pricing, shared by billing (TransactionService, CardService) and the CostUpdated sender so the
    /// amount shown on a charger is the amount the driver is charged. Pure.
    /// </summary>
    public interface ICostCalculator
    {
        /// <summary>Running cost of an ongoing session: the higher of the energy-based and the time-based price.</summary>
        double RunningCost(Connector connector, double chargedKwh, double elapsedMinutes);
        /// <summary>Amount debited from the card when the session ends (time-based).</summary>
        double FinalCost(Connector connector, double chargedMinutes);
        double EnergyCost(Connector connector, double chargedKwh);
        double IdleCost(Connector connector, double idleMinutes);
    }

    public sealed class CostCalculator : ICostCalculator
    {
        /// <summary>For code that is not resolved from the container (the calculator is stateless).</summary>
        public static readonly CostCalculator Instance = new();

        public double RunningCost(Connector connector, double chargedKwh, double elapsedMinutes) =>
            Math.Max(EnergyCost(connector, chargedKwh), FinalCost(connector, elapsedMinutes));

        public double FinalCost(Connector connector, double chargedMinutes) => chargedMinutes * connector.PricePerMinute;

        public double EnergyCost(Connector connector, double chargedKwh) => chargedKwh * connector.PricePerKWh;

        public double IdleCost(Connector connector, double idleMinutes) => idleMinutes * connector.PricePerIdleMinute;
    }
}
