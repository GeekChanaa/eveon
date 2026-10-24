using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.Models
{
    public class Connector : IEntity
    {
    
        public int ID { get; set; }
        public int? ConnectorID{ get; set; }
        public int EvseID{ get; set; }
        public int? ChargePointID { get; set; }
        public ConnectorEnumType? ConnectorType { get; set; } = ConnectorEnumType.cType2;
        public double Power { get; set; } = 0 ;
        [Precision(18, 4)]
        public decimal PricePerKWh { get; set; } = 0;
        [Precision(18, 4)]
        public decimal PricePerIdleMinute { get; set; } = 0;
        [Precision(18, 4)]
        public decimal PricePerMinute { get; set; } = 0;
        [NotMapped]
        public decimal PricePerHour { get { return PricePerMinute*60;} }
        [NotMapped]
        public string ConnectorName { get { return "EvseID : "+EvseID+" - ConnectorID : "+ConnectorID;} }
        [Precision(18, 4)]
        public decimal CostPerKwh { get; set; } = 0;
        [Precision(18, 4)]
        public decimal FlatFee { get; set; }  = 0 ;
        public double MaxPower { get; set; }  = 100;
        public TimeSpan? StartTime { get; set; }    
        public TimeSpan? EndTime { get; set; }      
        public ChargePoint? ChargePoint { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}