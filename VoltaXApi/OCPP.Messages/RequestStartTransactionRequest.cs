using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
    public class CustomDataType
    {
        [Required]
        [MaxLength(255)]
        public string VendorId { get; set; }
        public Dictionary<string, object> AdditionalProperties { get; set; } = new Dictionary<string, object>();
    }

    public enum ChargingProfileKindEnumType
    {
        Absolute,
        Recurring,
        Relative
    }

    public enum ChargingProfilePurposeEnumType
    {
        ChargingStationExternalConstraints,
        ChargingStationMaxProfile,
        TxDefaultProfile,
        TxProfile
    }

    public enum ChargingRateUnitEnumType
    {
        W,
        A
    }

    public class ChargingProfileType
    {
        [Required]
        public ChargingProfileKindEnumType Kind { get; set; }

        [Required]
        public ChargingProfilePurposeEnumType Purpose { get; set; }

        public string StackLevel { get; set; }
        
        [Required]
        public List<ChargingScheduleType> ChargingSchedule { get; set; }
    }

    public class ChargingScheduleType
    {
        [Required]
        public int Duration { get; set; }

        [Required]
        public int StartSchedule { get; set; }

        [Required]
        public ChargingRateUnitEnumType ChargingRateUnit { get; set; }

        public List<ChargingSchedulePeriodType> ChargingSchedulePeriod { get; set; }
    }

    public class ChargingSchedulePeriodType
    {
        [Required]
        public int StartPeriod { get; set; }

        [Required]
        public int Limit { get; set; }

        public int? NumberPhases { get; set; }
    }

    public class GetChargingProfilesRequestType
    {
        [Required]
        public string IdTag { get; set; }

        [Required]
        public ChargingProfilePurposeEnumType Purpose { get; set; }

        public int? StackLevel { get; set; }

        public DateTime? ChargingProfileStartDate { get; set; }

        public DateTime? ChargingProfileEndDate { get; set; }

        public int? ChargingProfileInterval { get; set; }
    }

    public class GetChargingProfilesResponseType
    {
        public List<ChargingProfileType> ChargingProfiles { get; set; }
    }

    public class SetChargingProfileRequestType
    {
        [Required]
        public string IdTag { get; set; }

        [Required]
        public ChargingProfileType ChargingProfile { get; set; }
    }

    public class SetChargingProfileResponseType
    {
        public string Status { get; set; }
    }
}
