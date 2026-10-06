using System.ComponentModel.DataAnnotations;
namespace VoltaXApi.Dtos
{
  public class UpdateConnectorFlatFeeDto
  {
    [Range(0, 10000)]
    public double FlatFee { get; set; }   
  }
}