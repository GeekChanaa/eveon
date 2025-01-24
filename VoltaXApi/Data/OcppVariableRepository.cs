using VoltaXApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using VoltaXApi.Dtos;
using AutoMapper;
using VoltaXApi.Exceptions;

namespace VoltaXApi.Data
{
  public class OcppVariableRepository : IOcppVariableRepository
  {
    private readonly VoltaXApiDbContext _context;

    public OcppVariableRepository(VoltaXApiDbContext context)
    {
      _context = context;
    }

    public async Task<OcppVariableInformationsDto> GetVariableByName(string name)
    {
      var variable = await _context.OcppVariables
        .Select(u => new OcppVariableInformationsDto{
          Name = u.Name,
          DataType = u.DataType,
          Unit = u.Unit,
          Description = u.Description,
        })
        .FirstOrDefaultAsync(v => v.Name == name);
      if(variable == null)
        throw new NotFoundException("No Variable with this name exists");
      
      var variableComponents = await this.GetVariableComponents(name);
      variable.AssociatedComponents = variableComponents;
      return variable;
    }

    public async Task<List<OcppComponentListDto>> GetVariableComponents(string Name)
    {
      var components = await _context.OcppVariableComponents
        .Where(u => u.Variable == Name)
        .Select(u => new OcppComponentListDto{Component = u.Component})
        .ToListAsync();

      return components;
    }

    public async Task<List<OcppVariableListDto>> GetVariables()
    {
      var variables = await _context.OcppVariables.Select(u => new OcppVariableListDto{
        Name = u.Name ,
        DataType = u.DataType , 
        Unit = u.Unit 
      }).ToListAsync();

      return variables;
    }

    public async Task<List<OcppVariableListDto>> GetVariablesByComponentNameAndInstance(string name, string instance)
    {
      return await _context.OcppVariableComponents
        .Where(u => u.Component == name && (u.Instance == "" || u.Instance ==null || u.Instance == instance))
        .Select(v => new OcppVariableListDto{
          Name = v.Variable,
          DataType = v.DataType,
          Unit = v.Unit
        })
        .ToListAsync();
    }

  }
}