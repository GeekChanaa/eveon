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
    public class OCPPConfigurationComponentRepository : Repository<OCPPConfigurationComponent>,IOCPPConfigurationComponentRepository
    {
        private readonly IMapper _mapper;
        public OCPPConfigurationComponentRepository(
            VoltaXApiDbContext context) : base(context)
        {
        }

        public async Task<OCPPConfigurationComponent> FindOrCreateComponent(ComponentType componentType)
        {
            // The EVSE is part of a component's identity: EVSE 1 and EVSE 2 "EVSE" components are different rows.
            int? evseId = componentType.Evse?.Id;
            int? connectorId = componentType.Evse?.ConnectorId;
            var component = await _context.OCPPConfigurationComponents
                .Include(c => c.OCPPConfigurationEVSE)
                .FirstOrDefaultAsync(c =>
                    c.Name == componentType.Name &&
                    c.Instance == componentType.Instance &&
                    (evseId == null
                        ? c.OCPPConfigurationEVSEID == null
                        : c.OCPPConfigurationEVSE != null && c.OCPPConfigurationEVSE.EVSEId == evseId && c.OCPPConfigurationEVSE.ConnectorId == connectorId));

            if (component == null)
            {
                component = new OCPPConfigurationComponent
                {
                    Name = componentType.Name,
                    Instance = componentType.Instance
                };

                if (componentType.Evse != null)
                {
                    component.OCPPConfigurationEVSE = new OCPPConfigurationEVSE
                    {
                        EVSEId = componentType.Evse.Id,
                        ConnectorId = componentType.Evse.ConnectorId
                    };
                }

                _context.OCPPConfigurationComponents.Add(component);
                await _context.SaveChangesAsync();
            }

            return component;
        }

    }
}

