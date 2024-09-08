

using System;
using System.Collections.Generic;

#nullable disable

namespace VoltaXApi.Models
{
    public class MessageLog  : IEntity
    {
        public int ID { get; set; }
        public DateTime LogTime { get; set; }
        public string? ChargePointId { get; set; }
        public int? ConnectorId { get; set; }
        public string Message { get; set; }
        public string Result { get; set; }
        public string ErrorCode { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
