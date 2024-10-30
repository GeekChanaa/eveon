using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{

    public class RequestStartTransactionRequest
    {
        public CustomDataType? CustomData { get; set; } 
        [Required]
        public IdTokenType IdToken { get; set; } 
        public List<AdditionalInfoType>? AdditionalInfo { get; set; }   
        public int? ConnectorId { get; set; } 
        public ChargingProfileType? ChargingProfile { get; set; } 
        public EVSEType? Evse { get; set; } 
        public int? RemoteStartId { get; set; } 
        public int? EvseId { get; set; } 
    }

}