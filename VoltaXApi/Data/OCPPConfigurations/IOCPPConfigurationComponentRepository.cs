using VoltaXApi.Dtos;
using VoltaXApi.Helpers;
using VoltaXApi.Models;
using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.Data
{
    public interface IOCPPConfigurationComponentRepository : IRepository<OCPPConfigurationComponent>
    {
        Task<OCPPConfigurationComponent> FindOrCreateComponent(ComponentType componentType);
    }
}