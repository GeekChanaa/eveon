using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.OCPP.Helpers
{
    /// <summary>Readings of one MeterValues / TransactionEvent message, in kWh, kW and %.</summary>
    public record MeterReading(double? EnergyKWh, double? PowerKW, double? StateOfCharge, DateTime? EnergyTimestamp)
    {
        public static readonly MeterReading Empty = new(null, null, null, null);

        /// <summary>Units that were neither energy nor power units; their scaled value was used as is.</summary>
        public IReadOnlyList<string> UnexpectedUnits { get; init; } = Array.Empty<string>();
    }

    /// <summary>
    /// OCPP 2.0.1 sampled value normalization. A sample's value is value * 10^multiplier in its unit
    /// (UnitOfMeasure.multiplier may be negative); a missing unit means Wh for energy and W for power.
    /// </summary>
    public static class MeterValueNormalizer
    {
        public static double ApplyMultiplier(double value, int multiplier) => multiplier switch
        {
            0 => value,
            // Dividing keeps decimal results exact where multiplying by 10^-n would not (1234 * 0.001).
            < 0 => value / Math.Pow(10, -multiplier),
            _ => value * Math.Pow(10, multiplier)
        };

        /// <summary>Converts an energy sample to kWh. Returns false for a unit that is not Wh/kWh (or VAh/varh variants).</summary>
        public static bool TryToKWh(double value, string? unit, int multiplier, out double kWh)
        {
            var scaled = ApplyMultiplier(value, multiplier);
            switch (unit?.Trim().ToLowerInvariant())
            {
                case null or "" or "wh" or "vah" or "varh":
                    kWh = scaled / 1000.0;
                    return true;
                case "kwh" or "kvah" or "kvarh":
                    kWh = scaled;
                    return true;
                default:
                    kWh = scaled;
                    return false;
            }
        }

        /// <summary>Converts a power sample to kW. Returns false for a unit that is not W/kW (or VA/var variants).</summary>
        public static bool TryToKW(double value, string? unit, int multiplier, out double kW)
        {
            var scaled = ApplyMultiplier(value, multiplier);
            switch (unit?.Trim().ToLowerInvariant())
            {
                case null or "" or "w" or "va" or "var":
                    kW = scaled / 1000.0;
                    return true;
                case "kw" or "kva" or "kvar":
                    kW = scaled;
                    return true;
                default:
                    kW = scaled;
                    return false;
            }
        }

        public static double ToKWh(double value, string? unit, int multiplier)
        {
            TryToKWh(value, unit, multiplier, out var kWh);
            return kWh;
        }

        public static double ToKW(double value, string? unit, int multiplier)
        {
            TryToKW(value, unit, multiplier, out var kW);
            return kW;
        }

        /// <summary>
        /// Extracts the energy register (kWh), active import power (kW) and state of charge from meter values.
        /// A sample without measurand is Energy.Active.Import.Register (OCPP default). Whole-meter samples win over
        /// per-phase ones, and a Transaction.End energy reading wins over every other energy reading.
        /// </summary>
        public static MeterReading Extract(IEnumerable<MeterValueType>? meterValues)
        {
            if (meterValues == null)
                return MeterReading.Empty;

            double? energy = null, power = null, soc = null;
            DateTime? energyTime = null;
            bool energyIsPhase = false, energyIsFinal = false, powerIsPhase = false;
            var unexpectedUnits = new List<string>();

            foreach (var meterValue in meterValues)
            {
                if (meterValue?.SampledValue == null)
                    continue;

                foreach (var sample in meterValue.SampledValue)
                {
                    if (sample == null)
                        continue;

                    var unit = sample.UnitOfMeasure?.Unit;
                    var multiplier = sample.UnitOfMeasure?.Multiplier ?? 0;
                    var isPhase = sample.Phase.HasValue;

                    switch (sample.Measurand ?? MeasurandEnumType.Energy_Active_Import_Register)
                    {
                        case MeasurandEnumType.Energy_Active_Import_Register:
                        case MeasurandEnumType.Missing:
                            var isFinal = sample.Context == ReadingContextEnumType.Transaction_End;
                            if (energyIsFinal && !isFinal)
                                break;
                            if (energy.HasValue && isPhase && !energyIsPhase && isFinal == energyIsFinal)
                                break;
                            if (!TryToKWh(sample.Value, unit, multiplier, out var kWh))
                                unexpectedUnits.Add(unit ?? string.Empty);
                            energy = kWh;
                            energyTime = meterValue.Timestamp;
                            energyIsPhase = isPhase;
                            energyIsFinal = isFinal;
                            break;

                        case MeasurandEnumType.Power_Active_Import:
                            if (power.HasValue && isPhase && !powerIsPhase)
                                break;
                            if (!TryToKW(sample.Value, unit, multiplier, out var kW))
                                unexpectedUnits.Add(unit ?? string.Empty);
                            power = kW;
                            powerIsPhase = isPhase;
                            break;

                        case MeasurandEnumType.SoC:
                            soc = ApplyMultiplier(sample.Value, multiplier);
                            break;
                    }
                }
            }

            return new MeterReading(energy, power, soc, energyTime) { UnexpectedUnits = unexpectedUnits };
        }
    }
}
