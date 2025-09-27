using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using VoltaXApi.Models;
using VoltaXApi.Helpers;
using VoltaXApi.Dtos;

namespace VoltaXApi.Data
{
    public interface IPartnerChargingStationRepository : IRepository<ChargingStation>
    {
        Task<IEnumerable<ChargingStationRevenue>> GetPartnerTop10ChargingStationsByRevenue(int partnerID, GlobalParams globalParams);
    }
}