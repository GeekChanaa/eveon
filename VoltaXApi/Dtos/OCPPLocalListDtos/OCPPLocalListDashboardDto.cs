

namespace VoltaXApi.Dtos;

public class OCPPLocalListDashboardDto
{
    public int ID { get; set; }
    public int Version { get; set; }
    public ICollection<OCPPLocalListItemDashboardDto>? OCPPLocalListItems { get; set; }

}