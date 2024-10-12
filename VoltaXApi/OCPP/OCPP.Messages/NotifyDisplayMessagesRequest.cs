using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


namespace VoltaXApi.OCPP.Messages
{
  public class NotifyDisplayMessagesRequestType
  {
      public CustomDataType CustomData { get; set; }

      [Required]
      public List<MessageInfoType> MessageInfo { get; set; }

      public int? RequestId { get; set; }

      public bool Tbc { get; set; } = false; // Default value
  }

  public class MessageInfoType
  {
      public CustomDataType CustomData { get; set; }

      [Required]
      public ComponentType Display { get; set; }

      [Required]
      public int Id { get; set; }

      [Required]
      public MessagePriorityEnumType Priority { get; set; }

      public MessageStateEnumType? State { get; set; }

      public DateTime? StartDateTime { get; set; }

      public DateTime? EndDateTime { get; set; }

      [MaxLength(36)]
      public string TransactionId { get; set; }

      [Required]
      public MessageContentType Message { get; set; }
  }

}