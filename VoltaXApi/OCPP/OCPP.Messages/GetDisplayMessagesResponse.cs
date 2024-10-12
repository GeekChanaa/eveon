using System;
using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
  
  public class GetDisplayMessagesResponse
  {
      [Required]
      public GetDisplayMessagesStatusEnumType Status { get; set; }

      public CustomDataType CustomData { get; set; }

      public StatusInfoType StatusInfo { get; set; }
  }

  public enum GetDisplayMessagesStatusEnumType
  {
      Accepted,
      Unknown
  }

}