using VoltaXApi.Dtos;
using VoltaXApi.Models;

namespace VoltaXApi.Data
{
    public interface IOcppComponentRepository
    {
        Task<List<OcppComponentListDto>> GetComponents();
        Task<OcppComponentInformationsDto> GetComponentByName(string Name);
        Task<List<OcppVariableListDto>> GetComponentVariables(string Name);
        Task<List<string>> GetComponentInstances(string name);
    }
}