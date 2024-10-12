using System.ComponentModel.DataAnnotations;


namespace VoltaXApi.OCPP.Messages
{
  public class ChargingProfileType
  {
    public CustomDataType customData { get; set; }

    [Required]
    public int id { get; set; }

    [Required]
    public int stackLevel { get; set; }

    [Required]
    public ChargingProfilePurposeEnumType chargingProfilePurpose { get; set; }

    [Required]
    public ChargingProfileKindEnumType chargingProfileKind { get; set; }

    public RecurrencyKindEnumType recurrencyKind { get; set; }

    public DateTime? validFrom { get; set; }

    public DateTime? validTo { get; set; }

    [Required]
    [MinLength(1)]
    [MaxLength(3)]
    public List<ChargingScheduleType> chargingSchedule { get; set; }

    [MaxLength(36)]
    public string transactionId { get; set; }
  }
}