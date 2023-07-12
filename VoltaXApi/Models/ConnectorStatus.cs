
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.Models
{
    public class ConnectorStatus : IEntity
    {   
        public int ID { get; set; }
        public string? ChargePointID { get; set; }
        public int? ConnectorID { get; set; }
        public string? LastStatus { get; set; }
        public DateTime? LastStatusTime { get; set; }
        public Connector? Connector { get; set; }
        public ChargePoint? ChargePoint { get; set; }
    }
}
