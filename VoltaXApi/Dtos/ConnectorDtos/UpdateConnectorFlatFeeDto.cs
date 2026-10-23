using System.ComponentModel.DataAnnotations;
namespace VoltaXApi.Dtos
{
  public class UpdateConnectorFlatFeeDto
  {
    [Range(0, 10000)]
    public decimal FlatFee { get; set; }   
  }
}