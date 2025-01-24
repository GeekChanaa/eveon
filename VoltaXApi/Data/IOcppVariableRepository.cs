using VoltaXApi.Dtos;
using VoltaXApi.Models;

namespace VoltaXApi.Data
{
    public interface IOcppVariableRepository
    {
        Task<List<OcppVariableListDto>> GetVariables();
        Task<OcppVariableInformationsDto> GetVariableByName(string Name);
        Task<List<OcppComponentListDto>> GetVariableComponents(string Name);
        Task<List<OcppVariableListDto>> GetVariablesByComponentNameAndInstance(string name, string instance);
    }
}