
namespace VoltaXApi.Models
{
    public class OCPPLocalListVersion  : IEntity
    {
        public int ID { get; set; }
        public int Version { get; set; }
        public int ChargePointID { get; set; }
        public ChargePoint? ChargePoint { get; set; }
        public ICollection<OCPPLocalListItem>? OCPPLocalListItems { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}