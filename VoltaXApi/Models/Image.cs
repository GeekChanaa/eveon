namespace VoltaXApi.Models
{
  public class Image :  IEntity
  {
    public int ID { get; set; }
    public string? Url { get; set; }
    public string? Description { get; set; }
    public DateTime UploadDate { get; set; }
    public string? Format { get; set; }
    public ImagePriorityEnum Priority { get; set; }
    public bool IsActive { get; set; }
    public string? AltText { get; set; }
    public bool IsDeleted { get ; set ; }
    public DateTime CreatedAt { get ; set ; }
    public DateTime UpdatedAt { get ; set ; }
    }
}