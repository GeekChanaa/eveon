
namespace VoltaXApi.Models
{
    public class Administrator  : IEntity
    {
        public int ID { get; set; }
        public int? UserID { get; set; }
        public User User { get; set; }

        public bool IsDeleted { get; set; } = false;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}