using VoltaXApi.Dtos;
using VoltaXApi.Helpers;
using VoltaXApi.Models;
using VoltaXApi.OCPP.Messages;

namespace VoltaXApi.Data
{
    public interface IOCPPConfigurationVariableRepository : IRepository<OCPPConfigurationVariable>
    {
        Task<OCPPConfigurationVariable> FindOrCreateVariable(VariableType variableModel);
    }
}