

using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.Dtos
{
  public class RatingListDto
  {
      public int ID { get; set; }

      public int Score { get; set; }

      public string? Comment { get; set; }

      [Required]
      public string? Username { get; set; }
  }
}