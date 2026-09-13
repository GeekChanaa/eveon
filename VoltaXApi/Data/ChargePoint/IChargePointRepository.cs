using VoltaxApi.Dtos;
using VoltaXApi.Dtos;
using VoltaXApi.Helpers;
using VoltaXApi.Models;

namespace VoltaXApi.Data
{
    public interface IChargePointRepository : IRepository<ChargePoint>
    {
        Task<List<Connector>> GetChargePointConnectors(int ChargePointID);
        Task<double> GetChargePointRevenue(string chargePointID, DateTime? startTime = null, DateTime? endTime= null);
        Task<double> GetPartnerChargePointRevenue(int partnerID,string chargePointID);
        Task AddAsync(ChargePoint chargePoint);
        Task<ChargePointDisplayDto> GetChargePointByIdAsync(int id);
        Task<List<ChargePointListDto>> GetChargingStationChargePoints(int chargingStationID);
        Task<bool> IsChargePointIDUnique(string chargePointID);
        Task<bool> IsChargePointSerialNumberUnique(string chargePointID);
        Task<ChargePointDisplayDto> GetChargePointByID(int chargePointID, ChargePointIncludableHelper includableHelper);
        Task<List<ChargePointSelectDto>> GetChargePointsIds();
        Task<ChargePoint> GetChargePointByChargePointIDAsync(string chargePointID);

        Task SetShowOnMap(int chargepointID, bool val);
        Task SetHasChargeCable(int chargepointID, bool val);
        IQueryable<ChargePointCRListDto> GetAllChargePoints(GlobalParams globalParams);
        IQueryable<ChargePointCRListDto> GetAllPartnerChargePoints(GlobalParams globalParams, int partnerID);
        Task<int> GetLatestChargePointNumberAsync();
        Task<ChargePointDetailsForMobileDto?> GetChargePointByQrCode(string qrCode);
    }
}