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

namespace VoltaXApi.Data
{
    public class ConnectorRepository : Repository<Connector>,IConnectorRepository
    {
        private readonly IMapper _mapper;
        public ConnectorRepository(
            VoltaXApiDbContext context,
            IMapper mapper) : base(context)
        {
          _mapper = mapper;
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

        public async Task<bool> UpdateConnectorPricing(int connectorID, UpdateConnectorPricingDto updateConnectorPricingDto)
        {
            var connector = await _context.Connectors.FirstOrDefaultAsync(c => c.ID == connectorID);

            if (connector == null)
            {
                return false; 
            }

            Console.WriteLine("PricePerKWh : "+  updateConnectorPricingDto.PricePerKWh);
            Console.WriteLine("PricePerMinute : "+  updateConnectorPricingDto.PricePerMinute);
            Console.WriteLine("PricePerHour : "+  updateConnectorPricingDto.PricePerHour);

            connector.PricePerKWh = updateConnectorPricingDto.PricePerKWh;
            connector.PricePerMinute = updateConnectorPricingDto.PricePerMinute;
            connector.PricePerHour = updateConnectorPricingDto.PricePerHour;

            _context.Connectors.Update(connector);
            int changes = await _context.SaveChangesAsync();

            return changes > 0;
        }
    }
}

