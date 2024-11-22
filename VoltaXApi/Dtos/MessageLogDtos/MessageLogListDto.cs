

using System;
using System.Collections.Generic;

namespace VoltaXApi.Dtos
{
    public class MessageLogListDto 
    {
        public int ID { get; set; }
        public DateTime LogTime { get; set; }
        public string? ChargePointId { get; set; }
        public int? ConnectorId { get; set; }
        public string Message { get; set; }
        public string Result { get; set; }
        public string? ErrorCode { get; set; }
        public string? ContentSent { get; set; }
        public string? ContentReceived { get; set; }
    }
}
