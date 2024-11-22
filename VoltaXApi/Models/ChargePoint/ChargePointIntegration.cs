using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.Models
{
    public class ChargePointIntegration  : IEntity
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string? Method { get; set; }
        public int ChargePointModelID { get; set; }
        public bool IsDeleted { get; set; } = false;
        public ChargePointModel? Model { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}