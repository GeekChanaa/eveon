namespace VoltaXApi.Models
{
    public interface IEntity
    {
        public int ID { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}