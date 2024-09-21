
namespace VoltaXApi.Models
{
  public class CommentImage : IEntity
  {
    public int ID { get; set; }
    public int CommentID { get; set; }
    public int ImageID { get; set; }  
    public int ImagePriority { get; set; }
    public Comment? Comment { get; set; }
    public Image? Image { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
  }
}