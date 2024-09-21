
namespace VoltaXApi.Models
{
  public class CommentReply : IEntity
  {
    public int ID { get; set; } 
    public int CommentID { get; set; }
    public string Reply { get; set; }
    public Comment? Comment { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}