using System.Globalization;
using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.OCPP.Ocpp16
{
    /// <summary>Conversions between OCPP 1.6 values and the (2.0.1-shaped) domain model.</summary>
    public static class Ocpp16Mapping
    {
        /// <summary>1.6 has no NoCredit / NotAtThisLocation...: anything that is not representable is Invalid.</summary>
        public static string ToIdTagStatus(AuthorizationStatusEnumType status) => status switch
        {
            AuthorizationStatusEnumType.Accepted => Ocpp16AuthorizationStatus.Accepted,
            AuthorizationStatusEnumType.Blocked => Ocpp16AuthorizationStatus.Blocked,
            AuthorizationStatusEnumType.Expired => Ocpp16AuthorizationStatus.Expired,
            AuthorizationStatusEnumType.ConcurrentTx => Ocpp16AuthorizationStatus.ConcurrentTx,
            _ => Ocpp16AuthorizationStatus.Invalid
        };

        /// <summary>1.6 ChargePointStatus to our connector status; null for an unknown value.</summary>
        public static ConnectorStatusEnumType? ToConnectorStatus(string? status) => status switch
        {
            "Available" => ConnectorStatusEnumType.Available,
            "Preparing" or "Charging" or "SuspendedEV" or "SuspendedEVSE" or "Finishing" => ConnectorStatusEnumType.Occupied,
            "Reserved" => ConnectorStatusEnumType.Reserved,
            "Unavailable" => ConnectorStatusEnumType.Unavailable,
            "Faulted" => ConnectorStatusEnumType.Faulted,
            _ => null
        };

        /// <summary>1.6 StopTransaction reasons that the customer is told about (as 2.0.1 abnormal stops are).</summary>
        public static bool IsAbnormalStopReason(string? reason) =>
            reason is "EmergencyStop" or "PowerLoss" or "Reboot" or "Other" or "HardReset" or "SoftReset";

        /// <summary>A timestamp as UTC whatever offset/kind it was read with.</summary>
        public static DateTime ToUtc(DateTime value) => value.Kind switch
        {
            DateTimeKind.Local => value.ToUniversalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
            _ => value
        };

        public static string ToTimestamp(DateTime value) => ToUtc(value).ToString("o", CultureInfo.InvariantCulture);

        /// <summary>
        /// 1.6 meter values in 2.0.1 form so MeterValueNormalizer applies the same rules (1.6 has no multiplier;
        /// the unit defaults to Wh). Signed and unparsable samples are skipped.
        /// </summary>
        public static List<MeterValueType> ToMeterValues(IEnumerable<MeterValue16>? meterValues)
        {
            var result = new List<MeterValueType>();
            foreach (var meterValue in meterValues ?? Enumerable.Empty<MeterValue16>())
            {
                var samples = new List<SampledValueType>();
                foreach (var sample in meterValue?.SampledValue ?? new List<SampledValue16>())
                {
                    if (sample == null || string.Equals(sample.Format, "SignedData", StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (!double.TryParse(sample.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                        continue;
                    var measurand = ParseEnum<MeasurandEnumType>(sample.Measurand);
                    if (sample.Measurand != null && measurand == null)
                        continue;
                    samples.Add(new SampledValueType
                    {
                        Value = value,
                        Measurand = measurand,
                        Context = ParseEnum<ReadingContextEnumType>(sample.Context),
                        Phase = ParseEnum<PhaseEnumType>(sample.Phase),
                        Location = ParseEnum<LocationEnumType>(sample.Location),
                        UnitOfMeasure = new UnitOfMeasureType { Unit = sample.Unit ?? DefaultUnit(measurand), Multiplier = 0 }
                    });
                }
                if (samples.Count > 0)
                    result.Add(new MeterValueType { Timestamp = ToUtc(meterValue!.Timestamp), SampledValue = samples });
            }
            return result;
        }

        private static string DefaultUnit(MeasurandEnumType? measurand) => measurand switch
        {
            MeasurandEnumType.Power_Active_Import or MeasurandEnumType.Power_Active_Export or MeasurandEnumType.Power_Offered => "W",
            MeasurandEnumType.SoC => "Percent",
            _ => "Wh"
        };

        // "Energy.Active.Import.Register" / "L1-N" / "Sample.Periodic" -> enum member names with underscores.
        private static T? ParseEnum<T>(string? value) where T : struct, Enum =>
            !string.IsNullOrWhiteSpace(value) && Enum.TryParse<T>(value.Replace('.', '_').Replace('-', '_'), ignoreCase: true, out var parsed)
                ? parsed
                : null;
    }
}
