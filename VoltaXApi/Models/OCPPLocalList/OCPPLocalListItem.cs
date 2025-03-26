
namespace VoltaXApi.Models
{
    public class OCPPLocalListItem  : IEntity
    {
        public int ID { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}