using VoltaXApi.Models;

namespace VoltaXApi.Data
{
    public interface IChargePointRepository : IRepository<ChargePoint>
    {
        Task<List<Connector>> GetChargePointConnectors(int ChargePointID);
        Task<double> GetChargePointRevenue(string chargePointID ,DateTime? start = null , DateTime? end = null);
    }
}