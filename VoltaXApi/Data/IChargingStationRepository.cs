using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using VoltaXApi.Models;
using VoltaXApi.Dtos;
using VoltaXApi.Helpers;

namespace VoltaXApi.Data
{
    public interface IChargingStationRepository : IRepository<ChargingStation>
    {
        Task<ChargingStationListDto> GetChargingStationByIdAsync(int chargingStationID);
        Task<ChargingStationListDto> GetChargingStationByIdAsync(int chargingStationID, ChargingStationIncludableHelper helper);
        Task<List<ChargingStationListDto>> GetPartnerChargingStationsList(int partnerID);
        
        new Task AddAsync(ChargingStation chargingStation);
        Task<double> GetChargingStationRevenue(int chargingStationID, DateTime? start = null , DateTime? end = null);
        Task<IEnumerable<double>> GetChargingStationRevenueLast7Days(int chargingStationID);
        Task<IEnumerable<double>> GetChargingStationRevenueLast30Days(int chargingStationID);
        Task<IEnumerable<double>> GetChargingStationRevenueLast12Months(int chargingStationID);
        Task<IEnumerable<ChargingStationRevenue>> GetTop10ChargingStationsByRevenue();

        // For Partner
        Task<double> GetPartnerChargingStationRevenue(int partnerID, int chargingStationID, DateTime? start = null , DateTime? end = null);
        Task<IEnumerable<double>> GetPartnerChargingStationRevenueLast7Days(int partnerID, int chargingStationID);
        Task<IEnumerable<double>> GetPartnerChargingStationRevenueLast30Days(int partnerID, int chargingStationID);
        Task<IEnumerable<double>> GetPartnerChargingStationRevenueLast12Months(int partnerID, int chargingStationID);
        Task<IEnumerable<ChargingStationRevenue>> GetPartnerTop10ChargingStationsByRevenue(int partnerID);

        Task<ChargingStation> CreateChargingStation(ChargingStationCreateDto chargingStationCreateDto);
        Task<List<ChargingStationSelectDto>> GetChargingStationNames(string searchTerm = "");
        Task<bool> ChargingStationExistsByName(string Name);

    }
}