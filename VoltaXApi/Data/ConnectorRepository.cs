using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;
using VoltaXApi.Models;
using VoltaXApi.Dtos;
using AutoMapper;
using VoltaXApi.Exceptions;

namespace VoltaXApi.Data
{
    public class ConnectorRepository : Repository<Connector>,IConnectorRepository
    {
        private readonly IMapper _mapper;
        private readonly GlobalConfigurations _globalConfig;
        public ConnectorRepository(
            VoltaXApiDbContext context,
            GlobalConfigurations globalConfigurations,
            IMapper mapper) : base(context)
        {
          _mapper = mapper;
          _globalConfig = globalConfigurations;
        }

        public async Task<List<ConnectorListDto>> GetChargePointConnectors(int chargePointID)
        {
            var connectors = await this._context.Connectors.Where(u => u.ChargePointID == chargePointID).ToListAsync();

            var connectorsDto = this._mapper.Map<List<Connector> , List<ConnectorListDto>>(connectors);
            return connectorsDto;
        }

        public async Task<List<ConnectorSelectDto>> GetConnectorsIds()
        {
            return await this._context.Connectors.Include(u => u.ChargePoint).Select(u => new ConnectorSelectDto {
                ChargePointID = u.ChargePoint.ChargePointId,
                ConnectorID = u.ConnectorID,
                ID = u.ID
            }).ToListAsync();
        }

        public async Task<List<ConnectorSelectDto>> GetChargePointConnectorIds(int chargePointID)
        {
            return await this._context.Connectors.Where(u => u.ChargePointID == chargePointID).Include(u => u.ChargePoint).Select(u => new ConnectorSelectDto {
                ChargePointID = u.ChargePoint.ChargePointId,
                ConnectorID = u.ConnectorID,
                ID = u.ID
            }).ToListAsync();
        }

        public async Task<bool> UpdateConnectorPricing(int connectorID, UpdateConnectorPricingDto updateConnectorPricingDto)
        {
            var connector = await _context.Connectors.FirstOrDefaultAsync(c => c.ID == connectorID);

            if (connector == null)
            {
                return false; 
            }

            connector.PricePerKWh = updateConnectorPricingDto.PricePerKWh;
            connector.PricePerIdleMinute = updateConnectorPricingDto.PricePerIdleMinute;
            connector.CostPerKwh = updateConnectorPricingDto.CostPerKwh;

            _context.Connectors.Update(connector);
            int changes = await _context.SaveChangesAsync();

            return changes > 0;
        }

        public async Task<bool> UpdateConnectorFlatFee(int connectorID, double flatFee)
        {
            var connector = await _context.Connectors.FirstOrDefaultAsync(c => c.ID == connectorID);

            if (connector == null)
            {
                return false; 
            }

            connector.FlatFee = flatFee;

            _context.Connectors.Update(connector);
            int changes = await _context.SaveChangesAsync();

            return changes > 0;
        }

        public async Task<Connector?> GetConnectorByConnectorIdEvseId(int? connectorId, int evseId, int chargePointID)
        {
            return await _context.Connectors
                            .Where(u=> u.ChargePointID == chargePointID && u.ConnectorID == connectorId && u.EvseID == evseId).FirstOrDefaultAsync();
        }
      
        public async Task ResetPricingChargePointConnectors(int chargePointID)
        {
            var connectors = await _context.Connectors.Where(c => c.ChargePointID == chargePointID).ToListAsync();
            foreach(var connector in connectors)
            {
                connector.PricePerKWh = (double) _globalConfig.DefaultPricePerKwh;
                connector.PricePerIdleMinute = (double) _globalConfig.DefaultIdleTimePricing;
                connector.CostPerKwh = (double) _globalConfig.DefaultCostPerKwh ;
                connector.FlatFee = (double) _globalConfig.DefaultFlatFee;
            }

            await _context.SaveChangesAsync();
        }

        public async Task ResetPricingConnector(int connectorID)
        {
            var connector = await _context.Connectors.Where(c => c.ID == connectorID).FirstOrDefaultAsync();
            if(connector == null) 
                throw new NotFoundException("No connector with this ID exists");
            connector.PricePerKWh = (double) _globalConfig.DefaultPricePerKwh;
            connector.PricePerIdleMinute = (double) _globalConfig.DefaultIdleTimePricing;
            connector.CostPerKwh = (double) _globalConfig.DefaultCostPerKwh ;
            connector.FlatFee = (double) _globalConfig.DefaultFlatFee;

            await _context.SaveChangesAsync();
        }

        public async Task<List<int>> GetChargePointEvsesIds(int chargePointID)
        {
            return await _context.Connectors
                .Where(u => u.ChargePointID == chargePointID)
                .Select(u => u.EvseID)
                .Distinct()
                .ToListAsync();
        }
        
    }
}

