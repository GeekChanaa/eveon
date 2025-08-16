
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.Models
{
    public class ConnectorStatus  : IEntity
    {   
        public int ID { get; set; }
        public int? ConnectorID { get; set; }
        public ConnectorStatusEnumType LastStatus { get; set; }
        public DateTime? LastStatusTime { get; set; }
        [ForeignKey(nameof(ConnectorID))]
        public Connector? Connector { get; set; }
        public ChargePoint? ChargePoint { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}