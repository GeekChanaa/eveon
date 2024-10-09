using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace VoltaXApi.OCPP.Messages
{
  public class IdTokenInfoType
  {
      [Required]
      public AuthorizationStatusEnumType Status { get; set; }

      public DateTime? CacheExpiryDateTime { get; set; }

      public int? ChargingPriority { get; set; }

      [StringLength(8)]
      public string Language1 { get; set; }

      [MinLength(1)]
      public List<int> EvseId { get; set; }

      public IdTokenType GroupIdToken { get; set; }

      [StringLength(8)]
      public string Language2 { get; set; }

      public MessageContentType PersonalMessage { get; set; }
      public CustomDataType CustomData { get; set; }
  }
}