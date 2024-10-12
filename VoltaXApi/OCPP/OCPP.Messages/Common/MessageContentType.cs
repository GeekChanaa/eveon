

using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
  public class MessageContentType
  {
      public CustomDataType CustomData { get; set; }

      [Required]
      public MessageFormatEnumType Format { get; set; }

      [MaxLength(8)]
      public string Language { get; set; }

      [Required]
      [MaxLength(512)]
      public string Content { get; set; }
  }

  public enum MessageFormatEnumType
  {
      ASCII,
      HTML,
      URI,
      UTF8
  }
}