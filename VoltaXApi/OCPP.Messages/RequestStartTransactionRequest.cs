using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
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
