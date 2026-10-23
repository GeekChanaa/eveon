using Newtonsoft.Json;
using VoltaXApi.OCPP.Helpers;
using VoltaXApi.OCPP.Messages;

namespace VoltaX.Tests.Ocpp;

/// <summary>Meter values from chargers: multipliers, units and which sample of a MeterValues message is the reading.</summary>
public class MeterValueNormalizerTests
{
    private static void Near(double expected, double? actual) =>
        Assert.True(actual.HasValue && Math.Abs(actual.Value - expected) < 1e-9 * Math.Max(1, Math.Abs(expected)), $"expected {expected}, got {actual}");

    private static MeterReading Extract(string json) =>
        MeterValueNormalizer.Extract(JsonConvert.DeserializeObject<List<MeterValueType>>(json));

    [Theory]
    [InlineData(-3)]
    [InlineData(-2)]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Multipliers_scale_by_a_power_of_ten_negative_ones_included(int multiplier)
    {
        var scaled = 1234 * Math.Pow(10, multiplier);

        Near(scaled, MeterValueNormalizer.ApplyMultiplier(1234, multiplier));
        Near(scaled / 1000, MeterValueNormalizer.ToKWh(1234, "Wh", multiplier));
        Near(scaled, MeterValueNormalizer.ToKWh(1234, "kWh", multiplier));
        Near(scaled / 1000, MeterValueNormalizer.ToKW(1234, "W", multiplier));
        Near(scaled, MeterValueNormalizer.ToKW(1234, "kW", multiplier));
    }

    [Fact]
    public void Conversions_are_exact_for_decimal_values()
    {
        Assert.Equal(1.234, MeterValueNormalizer.ToKWh(1234, "kWh", -3));
        Assert.Equal(15, MeterValueNormalizer.ToKWh(15000, "Wh", 0));
    }

    [Fact]
    public void A_missing_unit_defaults_to_Wh_or_W()
    {
        Near(5, MeterValueNormalizer.ToKWh(5000, null, 0));
        Near(5, MeterValueNormalizer.ToKWh(5000, "", 0));
        Near(7.4, MeterValueNormalizer.ToKW(7400, null, 0));
    }

    [Fact]
    public void Reactive_and_apparent_units_convert_like_active_ones_and_case_is_ignored()
    {
        Near(5, MeterValueNormalizer.ToKWh(5000, "varh", 0));
        Near(5, MeterValueNormalizer.ToKWh(5, "kvarh", 0));
        Near(5, MeterValueNormalizer.ToKWh(5000, "VAh", 0));
        Near(5, MeterValueNormalizer.ToKWh(5, "kVAh", 0));
        Near(7.4, MeterValueNormalizer.ToKW(7400, "VA", 0));
        Near(7.4, MeterValueNormalizer.ToKW(7.4, "kvar", 0));
        Near(11, MeterValueNormalizer.ToKW(11, "KW", 0));
    }

    [Fact]
    public void A_unit_that_is_not_energy_or_power_is_reported_and_kept_unconverted()
    {
        Assert.False(MeterValueNormalizer.TryToKWh(230, "V", 0, out var raw));
        Assert.Equal(230, raw);
        Assert.False(MeterValueNormalizer.TryToKW(16, "A", 1, out var rawPower));
        Assert.Equal(160, rawPower); // the multiplier still applies
    }

    [Fact]
    public void Energy_power_and_state_of_charge_are_extracted()
    {
        var reading = Extract("""
            [{ "timestamp": "2026-10-03T10:00:00Z", "sampledValue": [
                { "value": 12345, "measurand": "Energy.Active.Import.Register", "unitOfMeasure": { "unit": "Wh", "multiplier": 0 } },
                { "value": 7400, "measurand": "Power.Active.Import", "unitOfMeasure": { "unit": "W" } },
                { "value": 55, "measurand": "SoC", "unitOfMeasure": { "unit": "Percent" } } ] }]
            """);

        Near(12.345, reading.EnergyKWh);
        Near(7.4, reading.PowerKW);
        Near(55, reading.StateOfCharge);
        Assert.Equal(new DateTime(2026, 10, 3, 10, 0, 0, DateTimeKind.Utc), reading.EnergyTimestamp);
        Assert.Empty(reading.UnexpectedUnits);
    }

    [Fact]
    public void A_sample_without_measurand_and_unit_is_the_energy_register_in_Wh()
    {
        Near(0.042, Extract("""[{ "timestamp": "2026-10-03T10:00:00Z", "sampledValue": [ { "value": 42 } ] }]""").EnergyKWh);
    }

    [Fact]
    public void A_negative_multiplier_is_applied_on_extraction()
    {
        Near(12345.678, Extract("""
            [{ "timestamp": "2026-10-03T10:00:00Z", "sampledValue": [ { "value": 12345678, "unitOfMeasure": { "unit": "kWh", "multiplier": -3 } } ] }]
            """).EnergyKWh);
    }

    [Fact]
    public void The_Transaction_End_energy_wins_and_a_Transaction_End_SoC_is_not_taken_as_energy()
    {
        var reading = Extract("""
            [{ "timestamp": "2026-10-03T10:00:00Z", "sampledValue": [
                { "value": 100, "context": "Transaction.End", "measurand": "Energy.Active.Import.Register", "unitOfMeasure": { "unit": "kWh" } },
                { "value": 50, "context": "Transaction.End", "measurand": "SoC" },
                { "value": 99000, "context": "Sample.Periodic", "measurand": "Energy.Active.Import.Register", "unitOfMeasure": { "unit": "Wh" } } ] }]
            """);

        Near(100, reading.EnergyKWh);
        Near(50, reading.StateOfCharge);
    }

    [Fact]
    public void Whole_meter_energy_wins_over_a_per_phase_sample()
    {
        Near(30, Extract("""
            [{ "timestamp": "2026-10-03T10:00:00Z", "sampledValue": [
                { "value": 30000, "measurand": "Energy.Active.Import.Register" },
                { "value": 10000, "measurand": "Energy.Active.Import.Register", "phase": "L1" } ] }]
            """).EnergyKWh);
    }

    [Fact]
    public void No_meter_values_give_no_reading()
    {
        Assert.Equal(MeterReading.Empty, MeterValueNormalizer.Extract(null));
        Assert.Null(Extract("[]").EnergyKWh);
    }
}
