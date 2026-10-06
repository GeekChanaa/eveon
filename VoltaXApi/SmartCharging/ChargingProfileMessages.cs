using VoltaXApi.Exceptions;
using VoltaXApi.Models;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.OCPP.Models;
using V16 = VoltaXApi.OCPP.Ocpp16.SmartCharging;

namespace VoltaXApi.SmartCharging
{
    /// <summary>Validation and OCPP 2.0.1 / 1.6 message building of stored charging profiles. Pure (no I/O).</summary>
    public static class ChargingProfileMessages
    {
        public const int MaxPeriods = 1024;
        private const int SecondsPerDay = 86_400;
        private const int SecondsPerWeek = 7 * SecondsPerDay;

        /// <summary>Throws <see cref="ValidationException"/> when the profile cannot be sent to a charger speaking <paramref name="protocol"/>.</summary>
        public static void Validate(ChargingProfile profile, IReadOnlyList<ChargingProfilePeriod> periods, string protocol)
        {
            static void Fail(string message) => throw new ValidationException(message);

            if (profile.EvseId < 0) Fail("The EVSE id cannot be negative.");
            if (profile.StackLevel < 0) Fail("The stack level cannot be negative.");
            if (profile.Purpose == ChargingProfilePurposeEnum.ChargingStationExternalConstraints)
                Fail("ChargingStationExternalConstraints profiles are set by external systems, never by the CSMS.");
            if (profile.Purpose == ChargingProfilePurposeEnum.ChargingStationMaxProfile && profile.EvseId != 0)
                Fail("A ChargingStationMaxProfile applies to the whole charger (EVSE 0).");
            if (profile.Purpose == ChargingProfilePurposeEnum.TxProfile)
            {
                if (profile.EvseId == 0) Fail("A TxProfile targets the EVSE of a running transaction.");
                if (string.IsNullOrWhiteSpace(profile.TransactionId)) Fail("A TxProfile needs the transaction id.");
                if (protocol == OcppProtocols.Ocpp16 && ToOcpp16TransactionId(profile.TransactionId) == null)
                    Fail("The transaction is not an OCPP 1.6 transaction of this charger.");
            }
            else if (profile.TransactionId != null) Fail("Only a TxProfile carries a transaction id.");

            switch (profile.Kind)
            {
                case ChargingProfileKindEnum.Recurring:
                    if (profile.RecurrencyKind == null) Fail("A recurring profile needs Daily or Weekly recurrence.");
                    if (profile.StartSchedule == null) Fail("A recurring profile needs a schedule start.");
                    break;
                case ChargingProfileKindEnum.Absolute:
                    if (profile.StartSchedule == null) Fail("An absolute profile needs a schedule start.");
                    if (profile.RecurrencyKind != null) Fail("Only a recurring profile has a recurrence.");
                    break;
                case ChargingProfileKindEnum.Relative:
                    if (profile.StartSchedule != null) Fail("A relative profile starts with the transaction: no schedule start.");
                    if (profile.RecurrencyKind != null) Fail("Only a recurring profile has a recurrence.");
                    break;
            }

            if (profile.Duration is <= 0) Fail("The schedule duration must be positive.");
            if (profile.ValidFrom != null && profile.ValidTo != null && profile.ValidTo <= profile.ValidFrom)
                Fail("ValidTo must be after ValidFrom.");
            if (profile.MinChargingRate is < 0) Fail("The minimum charging rate cannot be negative.");

            if (periods.Count == 0) Fail("A schedule needs at least one period.");
            if (periods.Count > MaxPeriods) Fail($"A schedule has at most {MaxPeriods} periods.");
            if (periods[0].StartPeriod != 0) Fail("The first period must start at 0.");
            var cycle = profile.RecurrencyKind == ChargingProfileRecurrencyEnum.Weekly ? SecondsPerWeek : SecondsPerDay;
            for (var i = 0; i < periods.Count; i++)
            {
                var period = periods[i];
                if (i > 0 && period.StartPeriod <= periods[i - 1].StartPeriod) Fail("Period starts must be strictly increasing.");
                if (profile.Kind == ChargingProfileKindEnum.Recurring && period.StartPeriod >= cycle)
                    Fail("A period starts after the end of the recurrence cycle.");
                if (double.IsNaN(period.Limit) || double.IsInfinity(period.Limit) || period.Limit < 0) Fail("Limits must be zero or positive.");
                if (period.NumberPhases is < 1 or > 3) Fail("The number of phases is 1, 2 or 3.");
                if (period.PhaseToUse != null)
                {
                    if (period.PhaseToUse is < 1 or > 3) Fail("phaseToUse is 1, 2 or 3.");
                    if (period.NumberPhases != 1) Fail("phaseToUse requires numberPhases = 1.");
                    if (protocol == OcppProtocols.Ocpp16) Fail("phaseToUse does not exist in OCPP 1.6.");
                }
            }
        }

        /// <summary>The integer transactionId of a 1.6 transaction uid ("ocpp16-12" or "12").</summary>
        public static int? ToOcpp16TransactionId(string? transactionId) =>
            VoltaXApi.OCPP.Ocpp16.Ocpp16Transaction.TryParseUid(transactionId, out var id) ? id : null;

        public static SetChargingProfileRequest ToOcpp201(ChargingProfile profile, IReadOnlyList<ChargingProfilePeriod> periods) => new()
        {
            EvseId = profile.EvseId,
            ChargingProfile = new ChargingProfileType
            {
                Id = profile.OcppProfileId,
                StackLevel = profile.StackLevel,
                ChargingProfilePurpose = ToOcpp201(profile.Purpose),
                ChargingProfileKind = Enum.Parse<ChargingProfileKindEnumType>(profile.Kind.ToString()),
                RecurrencyKind = profile.RecurrencyKind == null ? null : Enum.Parse<RecurrencyKindEnumType>(profile.RecurrencyKind.ToString()!),
                ValidFrom = Utc(profile.ValidFrom),
                ValidTo = Utc(profile.ValidTo),
                TransactionId = profile.TransactionId,
                ChargingSchedule = new List<ChargingScheduleType>
                {
                    new()
                    {
                        Id = profile.OcppProfileId,
                        StartSchedule = Utc(profile.StartSchedule),
                        Duration = profile.Duration,
                        ChargingRateUnit = profile.ChargingRateUnit == ChargingRateUnitEnum.W ? ChargingRateUnitEnumType.W : ChargingRateUnitEnumType.A,
                        MinChargingRate = profile.MinChargingRate,
                        ChargingSchedulePeriod = periods.Select(p => new ChargingSchedulePeriodType
                        {
                            StartPeriod = p.StartPeriod,
                            Limit = RoundLimit(p.Limit),
                            NumberPhases = p.NumberPhases,
                            PhaseToUse = p.PhaseToUse
                        }).ToList()
                    }
                }
            }
        };

        public static V16.SetChargingProfileRequest ToOcpp16(ChargingProfile profile, IReadOnlyList<ChargingProfilePeriod> periods) => new()
        {
            ConnectorId = profile.EvseId,
            CsChargingProfiles = new V16.CsChargingProfiles
            {
                ChargingProfileId = profile.OcppProfileId,
                TransactionId = profile.Purpose == ChargingProfilePurposeEnum.TxProfile ? ToOcpp16TransactionId(profile.TransactionId) : null,
                StackLevel = profile.StackLevel,
                ChargingProfilePurpose = ToOcpp16(profile.Purpose),
                ChargingProfileKind = Enum.Parse<V16.ChargingProfileKindType>(profile.Kind.ToString()),
                RecurrencyKind = profile.RecurrencyKind == null ? null : Enum.Parse<V16.RecurrencyKindType>(profile.RecurrencyKind.ToString()!),
                ValidFrom = Utc(profile.ValidFrom),
                ValidTo = Utc(profile.ValidTo),
                ChargingSchedule = new V16.ChargingSchedule
                {
                    Duration = profile.Duration,
                    StartSchedule = Utc(profile.StartSchedule),
                    ChargingRateUnit = profile.ChargingRateUnit == ChargingRateUnitEnum.W ? V16.ChargingRateUnitType.W : V16.ChargingRateUnitType.A,
                    MinChargingRate = profile.MinChargingRate == null ? null : Math.Round((decimal)profile.MinChargingRate.Value, 1),
                    ChargingSchedulePeriod = periods.Select(p => new V16.ChargingSchedulePeriod
                    {
                        StartPeriod = p.StartPeriod,
                        Limit = Math.Round((decimal)p.Limit, 1, MidpointRounding.ToZero),
                        NumberPhases = p.NumberPhases
                    }).ToList()
                }
            }
        };

        public static ChargingProfilePurposeEnumType ToOcpp201(ChargingProfilePurposeEnum purpose) => purpose switch
        {
            ChargingProfilePurposeEnum.ChargingStationMaxProfile => ChargingProfilePurposeEnumType.ChargingStationMaxProfile,
            ChargingProfilePurposeEnum.TxDefaultProfile => ChargingProfilePurposeEnumType.TxDefaultProfile,
            ChargingProfilePurposeEnum.TxProfile => ChargingProfilePurposeEnumType.TxProfile,
            _ => ChargingProfilePurposeEnumType.ChargingStationExternalConstraints
        };

        public static ChargingProfilePurposeEnum FromOcpp201(ChargingProfilePurposeEnumType purpose) => purpose switch
        {
            ChargingProfilePurposeEnumType.ChargingStationMaxProfile => ChargingProfilePurposeEnum.ChargingStationMaxProfile,
            ChargingProfilePurposeEnumType.TxDefaultProfile => ChargingProfilePurposeEnum.TxDefaultProfile,
            ChargingProfilePurposeEnumType.TxProfile => ChargingProfilePurposeEnum.TxProfile,
            _ => ChargingProfilePurposeEnum.ChargingStationExternalConstraints
        };

        public static V16.ChargingProfilePurposeType ToOcpp16(ChargingProfilePurposeEnum purpose) => purpose switch
        {
            ChargingProfilePurposeEnum.ChargingStationMaxProfile => V16.ChargingProfilePurposeType.ChargePointMaxProfile,
            ChargingProfilePurposeEnum.TxDefaultProfile => V16.ChargingProfilePurposeType.TxDefaultProfile,
            ChargingProfilePurposeEnum.TxProfile => V16.ChargingProfilePurposeType.TxProfile,
            _ => throw new ValidationException("OCPP 1.6 has no ChargingStationExternalConstraints profiles.")
        };

        /// <summary>A profile reported by a 2.0.1 charger (ReportChargingProfiles), as a stored row and its periods (first schedule).</summary>
        public static (ChargingProfile Profile, List<ChargingProfilePeriod> Periods) FromOcpp201(ChargingProfileType reported, int evseId)
        {
            var schedule = reported.ChargingSchedule?.FirstOrDefault();
            var periods = schedule?.ChargingSchedulePeriod?
                .Select(p => new ChargingProfilePeriod(p.StartPeriod, p.Limit, p.NumberPhases, p.PhaseToUse))
                .OrderBy(p => p.StartPeriod)
                .ToList() ?? new List<ChargingProfilePeriod>();
            var profile = new ChargingProfile
            {
                EvseId = evseId,
                OcppProfileId = reported.Id,
                StackLevel = reported.StackLevel,
                Purpose = FromOcpp201(reported.ChargingProfilePurpose),
                Kind = Enum.Parse<ChargingProfileKindEnum>(reported.ChargingProfileKind.ToString()),
                RecurrencyKind = reported.RecurrencyKind == null ? null : Enum.Parse<ChargingProfileRecurrencyEnum>(reported.RecurrencyKind.ToString()!),
                ValidFrom = Utc(reported.ValidFrom),
                ValidTo = Utc(reported.ValidTo),
                TransactionId = reported.TransactionId,
                ChargingRateUnit = schedule?.ChargingRateUnit == ChargingRateUnitEnumType.W ? ChargingRateUnitEnum.W : ChargingRateUnitEnum.A,
                StartSchedule = Utc(schedule?.StartSchedule),
                Duration = schedule?.Duration,
                MinChargingRate = schedule?.MinChargingRate,
                PeriodsJson = SmartChargingJson.Serialize(periods)
            };
            return (profile, periods);
        }

        /// <summary>Limits are sent with one decimal (1.6 requires multiples of 0.1), rounded down so a limit is never exceeded.</summary>
        public static double RoundLimit(double limit) => Math.Floor(limit * 10 + 1e-9) / 10;

        private static DateTime? Utc(DateTime? value) => value switch
        {
            null => null,
            { Kind: DateTimeKind.Local } local => local.ToUniversalTime(),
            var v => DateTime.SpecifyKind(v.Value, DateTimeKind.Utc)
        };
    }
}
