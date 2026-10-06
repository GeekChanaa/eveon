using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.Dtos
{
    public class ConnectorCreateDto
    {
        [RegularExpression(@"^\d{1,3}$", ErrorMessage = "Connector id must be a number.")]
        public string ConnectorID { get; set; }
        [RegularExpression(@"^\d{1,3}$", ErrorMessage = "EVSE id must be a number.")]
        public string EvseID { get; set; }
        [StringLength(50)]
        public string? ConnectorType { get; set; }
        [Range(0, 1000)]
        public double Power { get; set; } = 0 ;
        [Range(0, 10000)]
        public double PricePerKWh { get; set; }   
        [Range(0, 10000)]
        public double FlatFee { get; set; }  = 0 ;     
        [Range(0, 10000)]
        public double PricePerMinute { get; set; }   
        [Range(0, 10000)]
        public double PricePerIdleMinute { get; set; }   
        [Range(0, 10000)]
        public double PricePerHour { get; set; }  
        [Range(0, 10000)]
        public double CostPerKwh { get; set; }  
        [Range(0, 1000)]
        public double MaxPower { get; set; }  
        public TimeSpan? StartTime { get; set; }    
        public TimeSpan? EndTime { get; set; }      
    
    }
}