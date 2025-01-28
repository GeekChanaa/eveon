using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
  public class SetDisplayMessageRequest
  {
      public CustomDataType? CustomData { get; set; }

      [Required]
      public MessageInfoType Message { get; set; }
  }

}