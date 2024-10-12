using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


namespace VoltaXApi.OCPP.Messages
{
  public class TransactionEventResponse
  {
      public CustomDataType CustomData { get; set; }

      public decimal? TotalCost { get; set; }

      public int? ChargingPriority { get; set; }

      public IdTokenInfoType IdTokenInfo { get; set; }

      public MessageContentType UpdatedPersonalMessage { get; set; }
  }

}