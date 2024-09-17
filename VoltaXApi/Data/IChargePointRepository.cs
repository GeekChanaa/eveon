using VoltaXApi.Dtos;
using VoltaXApi.Models;

namespace VoltaXApi.Data
{
    public interface IChargePointRepository : IRepository<ChargePoint>
    {
        Task<List<Connector>> GetChargePointConnectors(int ChargePointID);
        Task<double> GetChargePointRevenue(string chargePointID ,DateTime? start = null , DateTime? end = null);
        Task<double> GetPartnerChargePointRevenue(int partnerID,string chargePointID ,DateTime? start = null , DateTime? end = null);
        Task AddAsync(ChargePoint chargePoint);
        Task<ChargePointListDto> GetChargePointByIdAsync(int id);
        Task<List<ChargePointListDto>> GetChargingStationChargePoints(int chargingStationID);
        Task<bool> IsChargePointIDUnique(string chargePointID);
    }
}