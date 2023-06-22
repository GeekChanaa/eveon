
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.Models
{
    public class ConnectorStatus : IEntity
    {   
        public int ID { get; set; }
        public string ChargePointId { get; set; }
        public int ConnectorId { get; set; }
        public string? LastStatus { get; set; }
        public DateTime? LastStatusTime { get; set; }
    }
}
