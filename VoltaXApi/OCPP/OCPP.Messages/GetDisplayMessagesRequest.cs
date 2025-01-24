
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{

  public class GetDisplayMessagesRequest
  {
      [Required]
      public int RequestId { get; set; }

      public List<int>? Id { get; set; }

      public MessagePriorityEnumType? Priority { get; set; }

      public MessageStateEnumType? State { get; set; }

      public CustomDataType? CustomData { get; set; }
  }

}