using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


namespace VoltaXApi.OCPP.Messages
{
  public class ReserveNowRequest
  {
      public CustomDataType CustomData { get; set; }

      [Required]
      public int Id { get; set; }

      [Required]
      public DateTime ExpiryDateTime { get; set; }

      public ConnectorEnumType? ConnectorType { get; set; }

      [Required]
      public IdTokenType IdToken { get; set; }

      public int? EvseId { get; set; }

      public IdTokenType GroupIdToken { get; set; }
  }

  public enum ConnectorEnumType
  {
      cCCS1,
      cCCS2,
      cG105,
      cTesla,
      cType1,
      cType2,
      s309_1P_16A,
      s309_1P_32A,
      s309_3P_16A,
      s309_3P_32A,
      sBS1361,
      sCEE_7_7,
      sType2,
      sType3,
      Other1PhMax16A,
      Other1PhOver16A,
      Other3Ph,
      Pan,
      wInductive,
      wResonant,
      Undetermined,
      Unknown
  }

}