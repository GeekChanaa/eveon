using VoltaXApi.Models;

namespace VoltaXApi.Data
{
    public interface IChargePointRepository : IRepository<ChargePoint>
    {
        Task<List<Connector>> GetChargePointConnectors(int ChargePointID);
    }
}