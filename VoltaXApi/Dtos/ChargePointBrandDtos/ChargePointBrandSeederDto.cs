
namespace VoltaXApi.Dtos
{
  public class ChargePointBrandSeederDto
  {
    public int ID { get; set; }
    public string Name { get; set; }
    public string Identifier { get; set; }
    public List<ChargePointModelSeederDto> Models { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
  }
}