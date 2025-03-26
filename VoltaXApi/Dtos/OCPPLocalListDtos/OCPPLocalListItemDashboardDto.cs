

using VoltaXApi.Models;
using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.Dtos;

public class OCPPLocalListItemDashboardDto
{
    public int ID { get; set; }
    public string Token { get; set; }
    public IdTokenEnumType TokenType { get; set; }
    public AuthorizationStatusEnumType TokenStatus { get; set; }
    public OCPPLocalListVersion? OCPPLocalListVersion { get; set; }

}