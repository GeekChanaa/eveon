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
        public decimal PricePerKWh { get; set; }   
        [Range(0, 10000)]
        public decimal FlatFee { get; set; }  = 0 ;     
        [Range(0, 10000)]
        public decimal PricePerMinute { get; set; }   
        [Range(0, 10000)]
        public decimal PricePerIdleMinute { get; set; }   
        [Range(0, 10000)]
        public decimal PricePerHour { get; set; }  
        [Range(0, 10000)]
        public decimal CostPerKwh { get; set; }  
        [Range(0, 1000)]
        public double MaxPower { get; set; }  
        public TimeSpan? StartTime { get; set; }    
        public TimeSpan? EndTime { get; set; }      
    
    }
}