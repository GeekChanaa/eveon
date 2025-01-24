using VoltaXApi.Models;
using VoltaXApi.Data;
using VoltaXApi.Dtos;

namespace VoltaXApi.Services
{
  public class OcppComponentsVariablesService : IOcppComponentsVariablesService
  {

    private readonly IOcppComponentRepository _componentRepository;
    private readonly IOcppVariableRepository _variableRepository;

    public OcppComponentsVariablesService(
      IOcppComponentRepository componentRepo,
      IOcppVariableRepository variableRepo
    )
    {
      _componentRepository = componentRepo;
      _variableRepository = variableRepo;
    }

    public async Task<OcppComponentInformationsDto> GetComponentByName(string name)
    {
      return await this._componentRepository.GetComponentByName(name);
    }

    public async Task<List<OcppComponentListDto>> GetComponents()
    {
      return await this._componentRepository.GetComponents();
    }

    public async Task<OcppVariableInformationsDto> GetVariableByName(string name)
    {
      return await this._variableRepository.GetVariableByName(name);
    }

    public async Task<List<OcppVariableListDto>> GetVariables()
    {
      return await this._variableRepository.GetVariables();
    }

    public async Task<List<OcppVariableListDto>> GetVariablesByComponentNameAndInstance(string name, string? instance)
    {
      return await this._variableRepository.GetVariablesByComponentNameAndInstance(name , instance);
    }

    public async Task<List<string>> GetComponentInstances(string name)
    {
      return await this._componentRepository.GetComponentInstances(name);
    }

    public async Task<List<OcppVariableListDto>> GetComponentVariables(string name)
    {
      return await this._componentRepository.GetComponentVariables(name);
    }

  }

}