using VoltaXApi.Models;
using VoltaXApi.Dtos;

namespace VoltaXApi.Services
{
    public interface IOcppComponentsVariablesService
    {
        Task<List<OcppComponentListDto>> GetComponents();
        Task<OcppComponentInformationsDto> GetComponentByName(string name);
        Task<List<OcppVariableListDto>> GetVariables();
        Task<OcppVariableInformationsDto> GetVariableByName(string name);
        Task<List<OcppVariableListDto>> GetComponentVariables(string name);
        Task<List<string>> GetComponentInstances(string name);
        Task<List<OcppVariableListDto>> GetVariablesByComponentNameAndInstance(string name, string? instance);
    }
}