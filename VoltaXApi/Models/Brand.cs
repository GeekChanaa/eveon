namespace VoltaXApi.Models
{
  public class Brand
  {
      public long Id { get; set; }
      public string UrlHash { get; set; }
      public string Url { get; set; }
      public string Name { get; set; }
      public string Logo { get; set; }
      public DateTime? DeletedAt { get; set; }
      public DateTime CreatedAt { get; set; }
      public DateTime UpdatedAt { get; set; }
  }

}