using System;
using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{

  public class GetLogRequest
  {
      public CustomDataType? CustomData { get; set; }
      public LogParametersType? Log { get; set; }

      [Required]
      public LogEnumType LogType { get; set; }

      [Required]
      public int RequestId { get; set; }

      public int? Retries { get; set; }

      public int? RetryInterval { get; set; }
  }

  public enum LogEnumType
  {
      DiagnosticsLog,
      SecurityLog
  }

  public class LogParametersType
  {
      [Required]
      [MaxLength(512)]
      public string RemoteLocation { get; set; }
      public CustomDataType? CustomData { get; set; }
      public DateTime? OldestTimestamp { get; set; }
      public DateTime? LatestTimestamp { get; set; }
  }

}