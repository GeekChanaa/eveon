using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using VoltaXApi.Models;
using VoltaXApi.Dtos;

namespace VoltaXApi.Data
{
    public interface IChargingStationRepository : IRepository<ChargingStation>
    {
        new Task<ChargingStationListDto> GetChargingStationByIdAsync(int chargingStationID);
        new Task AddAsync(ChargingStation chargingStation);
        Task<double> GetChargingStationRevenue(int chargingStationID, DateTime? start = null , DateTime? end = null);
        Task<IEnumerable<double>> GetChargingStationRevenueLast7Days(int chargingStationID);
        Task<IEnumerable<double>> GetChargingStationRevenueLast30Days(int chargingStationID);
        Task<IEnumerable<double>> GetChargingStationRevenueLast12Months(int chargingStationID);
        Task<IEnumerable<ChargingStationRevenue>> GetTop10ChargingStationsByRevenue();
    }
}