using VoltaXApi.Dtos;
using VoltaXApi.Helpers;
using VoltaXApi.Models;
using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.Data
{
    public interface IOCPPConfigurationItemRepository : IRepository<OCPPConfigurationItem>
    {
        Task<int> SaveConfigurationsFromReportAsync(string chargePointId, NotifyReportRequest notifyReportRequest);
         IQueryable<OCPPConfigurationItemListDto> GetChargePointConfigurationItems(int chargePointID, GlobalParams globalParams);
    }
}