namespace VoltaXApi.Models
{
  public class CommentImage
  {
    public int ID { get; set; }
    public int CommentID { get; set; }
    public int ImageID { get; set; }  
    public int ImagePriority { get; set; }
    public Comment? Comment { get; set; }
    public Image? Image { get; set; }
  }
}