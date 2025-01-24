using VoltaXApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using VoltaXApi.Dtos;
using AutoMapper;
using VoltaXApi.Exceptions;

namespace VoltaXApi.Data
{
  public class OcppComponentRepository : IOcppComponentRepository
  {
    private readonly VoltaXApiDbContext _context;
    public OcppComponentRepository(VoltaXApiDbContext context)
    {
      _context = context;
    }

    public async Task<OcppComponentInformationsDto> GetComponentByName(string name)
    {
      var component = await _context.OcppComponents.Select(u => new OcppComponentInformationsDto{
        ID = u.ID,
        Component = u.Component,
        Description = u.Description
      }).FirstOrDefaultAsync(u => u.Component == name);

      if(component == null)
        throw new NotFoundException($"No Component by the name {name} exists");
      var componentVariables = await this.GetComponentVariables(name);
      component.AssociatedVariables = componentVariables;
      return component;

    }

    public async Task<List<OcppComponentListDto>> GetComponents()
    {
      var components = await _context.OcppComponents.Select(u => new OcppComponentListDto{
        Component = u.Component,
        ID = u.ID
      }).ToListAsync();

      return components;
    }

    public async Task<List<OcppVariableListDto>> GetComponentVariables(string name)
    {
      var variables = await _context.OcppVariableComponents
        .Where(u => u.Component == name).Select(u => new OcppVariableListDto {
          Name = u.Variable,
          DataType = u.DataType,
          Unit = u.Unit
        }).ToListAsync();

      return variables;
    }

    public async Task<List<string>> GetComponentInstances(string name)
    {
      return await _context.OcppVariableComponents
        .Where(u => u.Component == name && u.Instance != "" && u.Instance != null)
        .Select(u => u.Instance).ToListAsync();
    }
  }
}