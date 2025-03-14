using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using VoltaXApi.Helpers;
using VoltaXApi.Models;
using VoltaXApi.Dtos;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using VoltaXApi.OCPP.Messages;
using VoltaXApi.Exceptions;

namespace VoltaXApi.Data
{
    public class OCPPConfigurationVariableRepository : Repository<OCPPConfigurationVariable>,IOCPPConfigurationVariableRepository
    {
        private readonly IMapper _mapper;
        public OCPPConfigurationVariableRepository(
            VoltaXApiDbContext context) : base(context)
        {
        }

        public async Task<OCPPConfigurationVariable> FindOrCreateVariable(VariableType variableModel)
        {
            var variable = await _context.OCPPConfigurationVariables
                .FirstOrDefaultAsync(v => 
                    v.Name == variableModel.Name && 
                    v.Instance == variableModel.Instance);
            
            if (variable == null)
            {
                variable = new OCPPConfigurationVariable
                {
                    Name = variableModel.Name,
                    Instance = variableModel.Instance,
                };
                
                _context.OCPPConfigurationVariables.Add(variable);
                await _context.SaveChangesAsync();
            }
            
            return variable;
        }

    }
}

