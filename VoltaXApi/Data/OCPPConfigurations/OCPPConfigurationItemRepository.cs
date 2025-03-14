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
    public class OCPPConfigurationItemRepository : Repository<OCPPConfigurationItem>,IOCPPConfigurationItemRepository
    {
        private readonly IMapper _mapper;
        private readonly IOCPPConfigurationComponentRepository _componentRepository;
        private readonly IOCPPConfigurationVariableRepository _variableRepository;
        public OCPPConfigurationItemRepository(
            VoltaXApiDbContext context,
            IOCPPConfigurationComponentRepository componentRepository,
            IOCPPConfigurationVariableRepository variableRepository,
            IMapper mapper) : base(context)
        {
            _mapper = mapper;
            _componentRepository = componentRepository;
            _variableRepository = variableRepository;
        }

        public async Task<int> SaveConfigurationsFromReportAsync(string chargePointId, NotifyReportRequest notifyReportRequest)
        {
            int savedCount = 0;

            // Finding The ChargePointID
            var chargePoint = (await _context.ChargePoints.FirstOrDefaultAsync(cp => cp.ChargePointId == chargePointId));
            if(chargePoint == null) throw new ChargePointNotFoundException("There is no chargepoint with the id : "+chargePointId);

            foreach (var data in notifyReportRequest.ReportData)
            {
                // Find or create component
                var component = await _componentRepository.FindOrCreateComponent(data.Component);
                
                // Find or create variable
                var variable = await _variableRepository.FindOrCreateVariable(data.Variable);
                
                // Find existing configuration if any
                var existingConfig = await _context.OCPPConfigurationItems
                    .Include(c => c.OCPPConfigurationVariableAttributes)
                    .Include(c => c.OCPPConfigurationVariableCharacteristic)
                    .FirstOrDefaultAsync(c => 
                        c.ChargePointID == chargePoint.ID && 
                        c.OCPPConfigurationComponentID == component.ID && 
                        c.OCPPConfigurationVariableID == variable.ID);

                if (existingConfig == null)
                {
                    // Create new configuration
                    var newConfig = new OCPPConfigurationItem
                    {
                        ChargePointID = chargePoint.ID,
                        OCPPConfigurationComponentID = component.ID,
                        OCPPConfigurationVariableID = variable.ID,
                        OCPPConfigurationVariableAttributes = new List<OCPPConfigurationVariableAttribute>()
                    };

                    // Add variable attributes
                    foreach (var attrModel in data.VariableAttribute)
                    {
                        newConfig.OCPPConfigurationVariableAttributes.Add(new OCPPConfigurationVariableAttribute
                        {
                            Type = attrModel.Type,
                            Value = attrModel.Value,
                            Mutability = attrModel.Mutability,
                            Persistent = attrModel.Persistent,
                            Constant = attrModel.Constant
                        });
                    }

                    // Add variable characteristics if present
                    if (data.VariableCharacteristics != null)
                    {
                        newConfig.OCPPConfigurationVariableCharacteristic = new OCPPConfigurationVariableCharacteristic
                        {
                            Unit = data.VariableCharacteristics.Unit,
                            DataType = data.VariableCharacteristics.DataType,
                            MinLimit = data.VariableCharacteristics.MinLimit,
                            MaxLimit = data.VariableCharacteristics.MaxLimit,
                            ValuesList = data.VariableCharacteristics.ValuesList,
                            SupportsMonitoring = data.VariableCharacteristics.SupportsMonitoring,
                        };
                    }

                    _context.OCPPConfigurationItems.Add(newConfig);
                    savedCount++;
                }
                else
                {
                    // Update existing configuration
                    existingConfig.UpdatedAt = DateTime.UtcNow;

                    // Update variable attributes
                    // First, remove existing attributes
                    _context.OCPPConfigurationVariableAttributes.RemoveRange(existingConfig.OCPPConfigurationVariableAttributes);
                    
                    // Then add new attributes
                    existingConfig.OCPPConfigurationVariableAttributes = new List<OCPPConfigurationVariableAttribute>();
                    foreach (var attrModel in data.VariableAttribute)
                    {
                        existingConfig.OCPPConfigurationVariableAttributes.Add(new OCPPConfigurationVariableAttribute
                        {
                            Type = attrModel.Type,
                            Value = attrModel.Value,
                            Mutability = attrModel.Mutability,
                            Persistent = attrModel.Persistent,
                            Constant = attrModel.Constant
                        });
                    }

                    // Update variable characteristics if present
                    if (data.VariableCharacteristics != null)
                    {
                        if (existingConfig.OCPPConfigurationVariableCharacteristic == null)
                        {
                            existingConfig.OCPPConfigurationVariableCharacteristic = new OCPPConfigurationVariableCharacteristic
                            {
                                Unit = data.VariableCharacteristics.Unit,
                                DataType = data.VariableCharacteristics.DataType,
                                MinLimit = data.VariableCharacteristics.MinLimit,
                                MaxLimit = data.VariableCharacteristics.MaxLimit,
                                ValuesList = data.VariableCharacteristics.ValuesList,
                                SupportsMonitoring = data.VariableCharacteristics.SupportsMonitoring,
                            };
                        }
                        else
                        {
                            existingConfig.OCPPConfigurationVariableCharacteristic.Unit = data.VariableCharacteristics.Unit;
                            existingConfig.OCPPConfigurationVariableCharacteristic.DataType = data.VariableCharacteristics.DataType;
                            existingConfig.OCPPConfigurationVariableCharacteristic.MinLimit = data.VariableCharacteristics.MinLimit;
                            existingConfig.OCPPConfigurationVariableCharacteristic.MaxLimit = data.VariableCharacteristics.MaxLimit;
                            existingConfig.OCPPConfigurationVariableCharacteristic.ValuesList = data.VariableCharacteristics.ValuesList;
                            existingConfig.OCPPConfigurationVariableCharacteristic.SupportsMonitoring = data.VariableCharacteristics.SupportsMonitoring;
                        }
                    }
                    else if (existingConfig.OCPPConfigurationVariableCharacteristic != null)
                    {
                        _context.OCPPConfigurationVariableCharacteristics.Remove(existingConfig.OCPPConfigurationVariableCharacteristic);
                        existingConfig.OCPPConfigurationVariableCharacteristic = null;
                    }

                    savedCount++;
                }
            }

            await _context.SaveChangesAsync();
            return savedCount;
        }

        // Helper methods
        public IQueryable<OCPPConfigurationItemListDto> GetChargePointConfigurationItems(int chargePointID, GlobalParams globalParams)
        {
            var configurations = GetAllAsync(globalParams).Where(c => c.ChargePointID == chargePointID).Select(cp => new OCPPConfigurationItemListDto{
                ID = cp.ID,
                ComponentName = cp.OCPPConfigurationComponent.Name,
                VariableName = cp.OCPPConfigurationVariable.Name,
            });
            return configurations;
        }

    }
}

