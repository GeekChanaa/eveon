
using VoltaXApi.Models;

namespace VoltaXApi.Dtos;

public class ChargePointCRListDto
{
    public int ID  { get; set; }
    public string ChargePointId  { get; set; }
    public string Name  { get; set; }
    public string ChargingStationName  { get; set; }
    public string SerialNumber  { get; set; }
    public ChargePointCategoryEnum Category  { get; set; }
    public ChargePointStatusEnum Status  { get; set; }
    public string PartnerName  { get; set; }
}