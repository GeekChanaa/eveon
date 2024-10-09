using System.ComponentModel.DataAnnotations;
namespace VoltaXApi.OCPP.Messages
{
  public class ConsumptionCostType
  {
      [Required]
      public double StartValue { get; set; }

      [Required]
      [MinLength(1)]
      [MaxLength(3)]
      public List<CostType> Cost { get; set; }

      public CustomDataType CustomData { get; set; }
  }
}