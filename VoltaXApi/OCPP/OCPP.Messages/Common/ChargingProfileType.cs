using System.ComponentModel.DataAnnotations;

namespace VoltaXApi.OCPP.Messages
{
  public class ChargingProfileType
  {
    public CustomDataType? CustomData { get; set; }

    [Required]
    public int Id { get; set; }

    [Required]
    [Range(0, int.MaxValue)]
    public int StackLevel { get; set; }

    [Required]
    public ChargingProfilePurposeEnumType ChargingProfilePurpose { get; set; }

    [Required]
    public ChargingProfileKindEnumType ChargingProfileKind { get; set; }

    public RecurrencyKindEnumType? RecurrencyKind { get; set; }

    public DateTime? ValidFrom { get; set; }

    public DateTime? ValidTo { get; set; }

    [Required]
    [MinLength(1)]
    [MaxLength(3)]
    public List<ChargingScheduleType> ChargingSchedule { get; set; }

    [MaxLength(36)]
    public string? TransactionId { get; set; }
  }
}
