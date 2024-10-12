using System.ComponentModel.DataAnnotations;


namespace VoltaXApi.OCPP.Messages
{
  public class SalesTariffType
  {
      [Required]
      public int Id { get; set; }

      [Required]
      public string Name { get; set; }

      public List<ConsumptionCostType> ConsumptionCosts { get; set; }

      public double? FixedCost { get; set; }

      public double? VariableCost { get; set; }

      public CustomDataType CustomData { get; set; }
  }
}