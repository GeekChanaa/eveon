namespace VoltaXApi.Models
{
  public class CommentReply
  {
    public int ID { get; set; } 
    public int CommentID { get; set; }
    public string Reply { get; set; }
    public Comment? Comment { get; set; }
  }
}