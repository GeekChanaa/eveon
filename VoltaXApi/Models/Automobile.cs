namespace VoltaXApi
{
  public class Automobile
  {
      public long ID { get; set; }
      public string UrlHash { get; set; }
      public string Url { get; set; }
      public long BrandId { get; set; }
      public string Name { get; set; }
      public string Description { get; set; }
      public string PressRelease { get; set; }
      public string Photos { get; set; }
      public DateTime CreatedAt { get; set; }
      public DateTime UpdatedAt { get; set; }
  }

}