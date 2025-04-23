using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using Newtonsoft.Json;
using VoltaXApi.Models;
using VoltaXApi.OCPP.Messages;

namespace OCPP.Core.Server
{
    public class ChargePointStatus
    {
        private Dictionary<int, OnlineConnectorStatus> _onlineConnectors;

        public ChargePointStatus()
        {
        }

        public ChargePointStatus(ChargePoint chargePoint)
        {
            Id = chargePoint.ChargePointId;
        }

        public string Id { get; set; }

        public string Protocol { get; set; }

        public Dictionary<int, OnlineConnectorStatus> OnlineConnectors
        {
            get
            {
                if (_onlineConnectors == null)
                {
                    _onlineConnectors = new Dictionary<int, OnlineConnectorStatus>();
                }
                return _onlineConnectors;
            }
            set
            {
                _onlineConnectors = value;
            }
        }

        [JsonIgnore]
        public WebSocket WebSocket { get; set; }
    }

    public class OnlineConnectorStatus
    {
        public ConnectorStatusEnumType Status { get; set; }

        public double? ChargeRateKW { get; set; }

        public double? MeterKWH { get; set; }

        public double? SoC { get; set; }
    }

    public enum ConnectorStatusEnum
    {
        [System.Runtime.Serialization.EnumMember(Value = @"")]
        Undefined = 0,

        [System.Runtime.Serialization.EnumMember(Value = @"Available")]
        Available = 1,

        [System.Runtime.Serialization.EnumMember(Value = @"Occupied")]
        Occupied = 2,

        [System.Runtime.Serialization.EnumMember(Value = @"Unavailable")]
        Unavailable = 3,

        [System.Runtime.Serialization.EnumMember(Value = @"Faulted")]
        Faulted = 4
    }
}
